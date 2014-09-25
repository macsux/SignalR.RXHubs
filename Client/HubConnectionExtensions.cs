using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Contract;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using ConnectionState = Microsoft.AspNet.SignalR.Client.ConnectionState;

namespace Client
{
    public static class HubConnectionExtensions
    {
        private interface IDummy
        {
             
        }
        public static IObservable<TMessage> HubSubscriptionAsObservable<TMessage, THub>(this HubConnection connection, string hubName, Expression<Func<THub, Guid>> serverSubscribeMethod, ConnectionLostBehavior behavior) where THub : class, IHubSupportsObservables
        {
            IObservable<TMessage> clientObservable = Observable.Create<TMessage>(async observer =>
            {
                var disposables = new CompositeDisposable();
                var stateChangeObservable = Observable.FromEvent<StateChange>(
                    c => connection.StateChanged += c,
                    c => connection.StateChanged -= c);

                var currentConnection =
                    Observable.Create<Unit>(
                        o =>
                        {
                            if (connection.State == ConnectionState.Connected)
                                o.OnNext(new Unit());
                            else 
                                o.OnCompleted();
                            return () => { };
                        });

                var newConnectionObservable =
                    stateChangeObservable.Where(
                        x => x.NewState == ConnectionState.Connected && x.OldState != ConnectionState.Reconnecting).Select(x => new Unit())
                        .Merge(currentConnection);

                var closedObservable = Observable.FromEvent(c => connection.Closed += c, c => connection.Closed -= c);
                
                var typedProxy = connection.CreateHubProxy<THub, IDummy>(hubName);

                // if we're already connected, instantly subscribe
                Guid observableId = Guid.Empty;
                
                disposables.Add(newConnectionObservable.Subscribe(
                    async x =>
                        observableId = await AddSubscription(connection, hubName, serverSubscribeMethod, observer)));
                
                switch (behavior)
                {
                    case ConnectionLostBehavior.Error:
                        disposables.Add(closedObservable.Subscribe(o => observer.OnError(new Exception("Connection to server lost"))));
                        break;
                    case ConnectionLostBehavior.Complete:
                        disposables.Add(closedObservable.Subscribe(o => observer.OnCompleted()));
                        break;
                }

                return async () =>
                {
                    disposables.Dispose();
                    // only do explicit unsubscribe if we actually subscribed before and still connected
                    // server will do it's own cleanup
                    if (connection.State == ConnectionState.Connected && observableId != Guid.Empty)
                    {
                        await typedProxy.CallAsync(hub => hub.Unsubscribe(observableId))
                            .ContinueWith(removalTask =>
                            {
                                if (removalTask.IsFaulted)
                                    Console.WriteLine(removalTask.Exception);
                            });
                    }
                };
            }).Publish().RefCount();
            return clientObservable;
        }

        private static async Task<Guid> AddSubscription<TMessage, THub>(HubConnection connection,  string hubName, Expression<Func<THub, Guid>> serverSubscribeMethod, IObserver<TMessage> observer) where THub : class
        {

            var typedProxy = connection.CreateHubProxy<THub, IDummy>(hubName);
            var proxy = connection.GetHubProxy(hubName) ?? connection.CreateHubProxy(hubName); ;
            //                if (connection.State != ConnectionState.Connected)
            //                    await connection.Start();

            var observableId = await typedProxy.CallAsync(serverSubscribeMethod);

            string onNextMessageName = string.Format("{0}-{1}", observableId, "OnNext");
            string onErrorMessageName = string.Format("{0}-{1}", observableId, "OnError");
            string onCompleteMessageName = string.Format("{0}-{1}", observableId, "OnComplete");
            proxy.On<TMessage>(onNextMessageName, observer.OnNext);
            proxy.On<Error>(onErrorMessageName, error => observer.OnError(new RemoteException(error)));
            proxy.On(onCompleteMessageName, observer.OnCompleted);

            return observableId;
        }
        private static IHubProxy GetHubProxy(this HubConnection hubConnection, string hubName)
        {
            FieldInfo field = hubConnection.GetType().GetField("_hubs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == (FieldInfo)null)
                throw new ConstraintException("Couldn't find \"_hubs\" field inside of the HubConnection.");
            var dictionary = (Dictionary<string, HubProxy>)field.GetValue((object)hubConnection);
            if (dictionary.ContainsKey(hubName))
                return (IHubProxy)dictionary[hubName];
            else
                return (IHubProxy)null;
        }
    }

    public enum ConnectionLostBehavior
    {
        Error,
        WaitForReconnect,
        Complete
    }
}