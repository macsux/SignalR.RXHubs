using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs.Client
{
    internal class ObservableHubProxyHelper<THub> : IDisposable where THub : class 
    {
        private readonly ConcurrentDictionary<Guid, IDisposable> _subscriptions = new ConcurrentDictionary<Guid, IDisposable>();
        private readonly HubConnection _connection;
        private readonly ConnectionLostBehavior _behavior;
        private readonly IHubProxy _hubProxy;
        private readonly IObservable<ObservableNotification> _observableOnNext;
        private readonly IObservable<ObservableNotification> _observableOnError;
        private readonly IObservable<Guid> _observableOnComplete;
        public IHubProxy HubProxy
        {
            get { return _hubProxy; }
        }

        
        public ObservableHubProxyHelper(HubConnection connection, string hubName, ConnectionLostBehavior behavior)
        {
            _connection = connection;
            _behavior = behavior;

            _hubProxy = connection.GetHubProxy(hubName) ?? connection.CreateHubProxy(hubName);

            _observableOnNext = _hubProxy.Observe("ObservableOnNext").Select(x => new ObservableNotification(x[0].ToObject<Guid>(), x[1])).Publish().RefCount();
            _observableOnError = _hubProxy.Observe("ObservableOnError").Select(x => new ObservableNotification(x[0].ToObject<Guid>(), x[1])).Publish().RefCount();
            _observableOnComplete = _hubProxy.Observe("ObservableOnComplete").Select(x => x[0].ToObject<Guid>()).Publish().RefCount();

        }

        public IObservable<TMessage> HubSubscriptionAsObservable<TMessage>(Expression<Func<THub, IObservable<TMessage>>> serverSubscribeMethod)
        {
            var actionDetails = serverSubscribeMethod.GetActionDetails();
            return HubSubscriptionAsObservable<TMessage>(actionDetails.MethodName, actionDetails.Parameters);
        }
        public IObservable<TMessage> HubSubscriptionAsObservable<TMessage>(string method, IEnumerable<object> parameters)
        {
            var methodLocal = method;
            var parametersLocal = parameters.ToList();
            IObservable<TMessage> clientObservable = Observable.Create<TMessage>(observer =>
            {
                var disposables = new CompositeDisposable();
                var stateChangeObservable = Observable.FromEvent<StateChange>(
                    c => _connection.StateChanged += c,
                    c => _connection.StateChanged -= c);

                var currentConnection =
                    Observable.Create<Unit>(
                        o =>
                        {
                            if (_connection.State == ConnectionState.Connected)
                                o.OnNext(new Unit());
                            else
                                o.OnCompleted();
                            return Disposable.Empty;
                        });

                var newConnectionObservable =
                    stateChangeObservable.Where(
                        x => x.NewState == ConnectionState.Connected && x.OldState != ConnectionState.Reconnecting)
                        .Select(x => new Unit())
                        .Merge(currentConnection);

                var closedObservable = Observable.FromEvent(c => _connection.Closed += c, c => _connection.Closed -= c);

                // if we're already connected, instantly subscribe
                Guid observableId = Guid.NewGuid();

                // start listening to the common "on-next" stream for messages with our id, and deserialize them into expected format
                disposables.Add(_observableOnNext.Where(x => x.SubscriptionId == observableId)
                    .Subscribe(msg => observer.OnNext(msg.Message.ToObject<TMessage>())));
                disposables.Add(_observableOnError.Where(x => x.SubscriptionId == observableId)
                    .Subscribe(msg =>
                {
                    // terminate subscription during error
                    observer.OnError(new RemoteException(msg.Message.ToObject<Error>()));
                    disposables.Dispose();
                }));
                disposables.Add(_observableOnComplete.Where(x => x == observableId).Subscribe(msg =>
                {
                    //terminate subscription when complete
                    observer.OnCompleted();
                    disposables.Dispose();
                }));

                // wait until we're connected, then add subscription
                disposables.Add(newConnectionObservable.Subscribe(async _ =>
                {
                    var subscriptionParameters = parametersLocal.ToList();
                    subscriptionParameters.Insert(0, observableId);
                    try
                    {
                        await _hubProxy.Invoke(methodLocal, subscriptionParameters.ToArray());
                    }
                    catch (Exception)
                    {
                        // TODO: Decide what to do with reconnection
                    }
                    
                }));

                switch (_behavior)
                {
                    case ConnectionLostBehavior.Error:
                        disposables.Add(closedObservable.Subscribe(o => observer.OnError(new Exception("Connection to server lost"))));
                        break;
                    case ConnectionLostBehavior.Complete:
                        disposables.Add(closedObservable.Subscribe(o => observer.OnCompleted()));
                        break;
                }
                _subscriptions.TryAdd(observableId, disposables);
                return async () =>
                {
                    disposables.Dispose();
                    IDisposable temp;
                    _subscriptions.TryRemove(observableId, out temp);
                    // only do explicit unsubscribe if we actually subscribed before and still connected
                    // server will do it's own cleanup
                    if (_connection.State == ConnectionState.Connected && observableId != Guid.Empty)
                    {
                        await _hubProxy.Invoke("Unsubscribe", observableId)
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
        private async Task AddSubscription<TMessage>(Guid subscriptionId, Expression<Func<THub, IObservable<TMessage>>> serverSubscribeMethod)
        {
            var actionDetails = serverSubscribeMethod.GetActionDetails();
            var subscriptionParameters = actionDetails.Parameters.ToList();
            subscriptionParameters.Insert(0, subscriptionId);
            await _hubProxy.Invoke(actionDetails.MethodName, subscriptionParameters.ToArray());

//            string onNextMessageName = string.Format("{0}-{1}", observableId, "OnNext");
//            string onErrorMessageName = string.Format("{0}-{1}", observableId, "OnError");
//            string onCompleteMessageName = string.Format("{0}-{1}", observableId, "OnComplete");
//            proxy.On<TMessage>(onNextMessageName, observer.OnNext);
//            proxy.On<Error>(onErrorMessageName, error => observer.OnError(new RemoteException(error)));
//            proxy.On(onCompleteMessageName, observer.OnCompleted);
//
//            return observableId;
        }

        public void Dispose()
        {
            _subscriptions.Values.ToList().ForEach(x => x.Dispose());
        }
    }
}