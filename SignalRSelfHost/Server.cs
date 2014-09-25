using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Contract;
using Dynamitey;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;

namespace SignalRSelfHost
{
    internal class Server
    {
        private static void Main(string[] args)
        {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses. 
            // See http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx 
            // for more information.
            string url = "http://*:8000";
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
        }
    }

    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }

    public class MyHub : Hub<IClient>, IServerHub
    {
        private static readonly ConcurrentDictionary<Tuple<string, Guid>, IDisposable> subscriptions =
            new ConcurrentDictionary<Tuple<string, Guid>, IDisposable>();

        public void Send(ClientMessage message)
        {
            Console.WriteLine("{2}: {0} > {1}", message.User, message.Message, Context.ConnectionId);
            Clients.All.AddMessage(message);
        }

        public Guid MsgSubscribe()
        {
            return SubscribeCallerToObservable(
                Observable.Interval(TimeSpan.FromSeconds(1))
                    .Select(x => new ClientMessage {Message = Guid.NewGuid().ToString(), User = "Server"})
                );
        }

        public void Unsubscribe(Guid observableId)
        {
            var subscriptionId = new Tuple<string, Guid>(Context.ConnectionId, observableId);
            Console.WriteLine("Unsubscribe called for {0}", subscriptionId);
            IDisposable subscription;
            if (subscriptions.TryRemove(subscriptionId, out subscription))
            {
                subscription.Dispose();
            }
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            Console.WriteLine("Client disconnected - removing subscriptions");
            subscriptions.Keys.Where(x => x.Item1 == Context.ConnectionId)
                .ToList()
                .ForEach(subscriptionId =>
                {
                    Console.WriteLine("Removing subscription {0}", subscriptionId);
                    IDisposable subscription;
                    if (subscriptions.TryRemove(subscriptionId, out subscription))
                        subscription.Dispose();
                });
        }

        public void RemoveMsg(string msgType)
        {
            Console.WriteLine("{0} removed", msgType);
        }

        public void AddMsg(string msgType)
        {
            Console.WriteLine("{0} added", msgType);
        }

        private Guid SubscribeCallerToObservable<T>(IObservable<T> observable)
        {
            Guid observableId = Guid.NewGuid();
            Console.WriteLine("Subscribing client {0} to {1}", Context.ConnectionId, observableId);
            var clientSubscriptionId = new Tuple<string, Guid>(Context.ConnectionId, observableId);

            var hub = (IHub) this;
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
            subscriptions.TryAdd(clientSubscriptionId, subscription);
            return observableId;
        }
    }
}