using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
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
        public static IObservable<TMessage> HubSubscriptionAsObservable<TMessage, THub>(this HubConnection connection, string hubName, Expression<Func<THub, Guid>> serverSubscribeMethod) where THub : class, IHubSupportsObservables
        {
            IObservable<TMessage> clientObservable = Observable.Create<TMessage>(async observer =>
            {
                var typedProxy = connection.CreateHubProxy<THub, IClient>(hubName);
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

                return async () => await typedProxy.CallAsync(hub => hub.Unsubscribe(observableId))
                    .ContinueWith(removalTask =>
                    {
                        if (removalTask.IsFaulted)
                            Console.WriteLine(removalTask.Exception);
                    });
            }).Publish().RefCount();
            return clientObservable;
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
}