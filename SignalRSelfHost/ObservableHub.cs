using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Contract;
using Dynamitey;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalRSelfHost
{
    public abstract class ObservableHub<T> : Hub<T>, IHubSupportsObservables, IVirtualHub where T : class
    {

        private static readonly ConcurrentDictionary<Tuple<string, Guid>, IDisposable> _subscriptions = new ConcurrentDictionary<Tuple<string, Guid>, IDisposable>();

        public void Unsubscribe(Guid observableId)
        {
            var subscriptionId = new Tuple<string, Guid>(Context.ConnectionId, observableId);
            Console.WriteLine("Unsubscribe called for {0}", subscriptionId);
            IDisposable subscription;
            if (_subscriptions.TryRemove(subscriptionId, out subscription))
            {
                subscription.Dispose();
            }
        }

        public Guid SubscribeCallerToObservable<T>(IObservable<T> observable)
        {
            Guid observableId = Guid.NewGuid();
            Console.WriteLine("Subscribing client {0} to {1}", Context.ConnectionId, observableId);
            var clientSubscriptionId = new Tuple<string, Guid>(Context.ConnectionId, observableId);

            var hub = (IHub)this;
            Action<T> onNext =
                param =>
                    Dynamic.InvokeMemberAction(hub.Clients.Caller, string.Format("{0}-{1}", observableId, "OnNext"),
                        param);
            Action<Exception> onError =
                exception =>
                    Dynamic.InvokeMemberAction(hub.Clients.Caller, string.Format("{0}-{1}", observableId, "OnError"),
                        new Error(exception));
            Action onComplete =
                () =>
                    Dynamic.InvokeMemberAction(hub.Clients.Caller, string.Format("{0}-{1}", observableId, "OnComplete"));
            IDisposable subscription = observable.Subscribe(onNext, onError, onComplete);
            _subscriptions.TryAdd(clientSubscriptionId, subscription);
            return observableId;
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            Console.WriteLine("Client disconnected - removing subscriptions");
            _subscriptions.Keys.Where(x => x.Item1 == Context.ConnectionId)
                .ToList()
                .ForEach(subscriptionId =>
                {
                    Console.WriteLine("Removing subscription {0}", subscriptionId);
                    IDisposable subscription;
                    if (_subscriptions.TryRemove(subscriptionId, out subscription))
                        subscription.Dispose();
                });
        }
        
        
    }
}