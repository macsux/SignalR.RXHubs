using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Practices.ServiceLocation;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs
{
    public abstract class ObservableHub<T> : Hub<T>, IVirtualHub where T : class
    {
        private static readonly ConcurrentDictionary<Tuple<string, Guid>, IObservableDispatch> _subscriptions = new ConcurrentDictionary<Tuple<string, Guid>, IObservableDispatch>();
        private readonly Func<Guid, Action<ObservableNotification>, IDisposable, IObservableDispatch> _observableDispatchFactory;
        protected ObservableHub()
            : this(ServiceLocator.Current.GetInstance<Func<Guid, Action<ObservableNotification>, IDisposable, IObservableDispatch>>())
        {
            
        }
        protected ObservableHub(Func<Guid, Action<ObservableNotification>, IDisposable, IObservableDispatch> observableDispatchFactory)
        {
            _observableDispatchFactory = observableDispatchFactory;
        }
        
        
        public void Unsubscribe(Guid observableId)
        {
            var subscriptionId = new Tuple<string, Guid>(Context.ConnectionId, observableId);
            Console.WriteLine("Unsubscribe called for {0}", subscriptionId);
            IObservableDispatch subscription;
            if (_subscriptions.TryRemove(subscriptionId, out subscription))
            {
                subscription.Subscription.Dispose();
            }
        }

        public void Ack(Guid observableId, int messageId)
        {
            var subscriptionId = new Tuple<string, Guid>(Context.ConnectionId, observableId);
            IObservableDispatch dispatch;
            if (_subscriptions.TryGetValue(subscriptionId, out dispatch))
            {
                dispatch.Ack(messageId);
            }
        }

        public void SubscribeCallerToObservable<TMsg>(Guid observableId, IObservable<TMsg> observable)
        {
            Console.WriteLine("Subscribing client {0} to {1}", Context.ConnectionId, observableId);
            var clientSubscriptionId = new Tuple<string, Guid>(Context.ConnectionId, observableId);

            var hub = (IHub)this;
            var client = hub.Clients.Caller;
            
            var observableSubscription = new CompositeDisposable();
            var dispatcher = _observableDispatchFactory(observableId, notification => client.O(notification), observableSubscription);

            _subscriptions.TryAdd(clientSubscriptionId, dispatcher);
            observableSubscription.Add(observable.Subscribe(x => dispatcher.OnNext(x), dispatcher.OnError, dispatcher.OnCompleted));

        }

        private void RemoveSubscription(Tuple<string,Guid> subscriptionId)
        {
            IObservableDispatch dispatch;
            if (_subscriptions.TryRemove(subscriptionId, out dispatch))
                dispatch.Subscription.Dispose();
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            await Task.Run(() =>
            {
                Console.WriteLine("Client disconnected - removing subscriptions");
                _subscriptions.Where(x => x.Key.Item1 == Context.ConnectionId)
                    .ToList()
                    .ForEach(observable =>
                    {
                        Console.WriteLine("Removing subscription {0}", observable.Key);
                        RemoveSubscription(observable.Key);
                    });
            });
        }
        
    }
}