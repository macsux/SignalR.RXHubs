using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.SignalR;
using Castle.DynamicProxy;
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

    public class EmptyInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            
        }
    }

    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var generator = new ProxyGenerator();
            var sourceType = typeof(IServerHub);
            TypeBuilder typeBuilder = generator.ProxyBuilder.ModuleScope.DefineType(true, sourceType.Name, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface);
            typeBuilder.AddInterfaceImplementation(typeof(IHub));
            foreach (var method in sourceType.GetMethods())
            {
                var returnType = method.ReturnType;
                if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    returnType = typeof(Guid);
                }
                typeBuilder.DefineMethod(method.Name, method.Attributes, returnType,
                        method.GetParameters().Select(x => x.ParameterType).ToArray());
            }
            var unsubscribeMethod = typeof (ObservableHub<>).GetMethod("Unsubscribe");
            typeBuilder.DefineMethod(unsubscribeMethod.Name, MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.Virtual, unsubscribeMethod.ReturnType,
                unsubscribeMethod.GetParameters().Select(x => x.ParameterType).ToArray());
            var realHubInterfaceType = typeBuilder.CreateType();

            
            var containerBuilder = new ContainerBuilder();
//            var options = new ProxyGenerationOptions(new ObservableInterceptorSelector());
            containerBuilder.RegisterType<AutofacHubDescriptorProvider>().AsImplementedInterfaces();
            containerBuilder.RegisterType<MyHub>();
            var options = new ProxyGenerationOptions() {BaseTypeForInterfaceProxy = typeof (Hub<IClient>)};
            Type proxyType = generator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options, new EmptyInterceptor()).GetType();
            
            containerBuilder.Register(
                context =>
                {
                    var implementedHub = context.Resolve<MyHub>();
                    var retval = generator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType,options,
                         new ObservableInterceptor<IClient>(implementedHub));
                    return retval;
                }).As<IHub>().As(proxyType).ExternallyOwned();
            containerBuilder.RegisterHubs();
            var container = containerBuilder.Build();
//            container.Resolve<IHub>();
            var test = container.Resolve(proxyType);
            var resolver = new AutofacDependencyResolver(container);
            GlobalHost.DependencyResolver = resolver;

            var hubConfiguration = new HubConfiguration();
            hubConfiguration.EnableDetailedErrors = true;
            app.UseAutofacMiddleware(container);
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR(hubConfiguration);
        }
    }


    public class ObservableInterceptor<T> : IInterceptor where T : class
    {
        private readonly ObservableHub<T> _implementation;

        public ObservableInterceptor(ObservableHub<T> implementation)
        {
            _implementation = implementation;
        }

        public void Intercept(IInvocation invocation)
        {
            var targetMethod = _implementation.GetType()
                .GetMethod(invocation.Method.Name);

            
            if (invocation.Method.Name == "get_Clients")
            {
                invocation.ReturnValue = typeof(IHub).GetProperty("Clients").GetValue(_implementation);
            }
            else if (invocation.Method.Name == "set_Clients")
            {
                typeof(IHub).GetProperty("Clients").SetValue(_implementation,invocation.Arguments.First());
            }
            else if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                var observable = targetMethod.Invoke(_implementation, invocation.Arguments);
                var subscribeMethod = _implementation.GetType().GetMethod("SubscribeCallerToObservable").MakeGenericMethod(observable.GetType().GetGenericArguments().Last());
                invocation.ReturnValue = subscribeMethod.Invoke(_implementation, new[] {observable});
                //invocation.ReturnValue = _implementation.SubscribeCallerToObservable(observable);
            }
            
            else
            {
                invocation.ReturnValue =
                    _implementation.GetType()
                        .GetMethod(invocation.Method.Name)
                        .Invoke(_implementation, invocation.Arguments);    
            }
        }
    }

    public abstract class ObservableHub<T> : Hub<T>, IHubSupportsObservables where T : class
    {

        private static readonly ConcurrentDictionary<Tuple<string, Guid>, IDisposable> _subscriptions =
            new ConcurrentDictionary<Tuple<string, Guid>, IDisposable>();

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


    public class MyHub : ObservableHub<IClient>, IServerHub
    {

        public void Send(ClientMessage message)
        {
            Console.WriteLine("{2}: {0} > {1}", message.User, message.Message, Context.ConnectionId);
            Clients.All.AddMessage(message);
        }

        public IObservable<ClientMessage> MsgSubscribe()
        {
            return 
                Observable.Interval(TimeSpan.FromSeconds(1))
                    .Select(x => new ClientMessage {Message = Guid.NewGuid().ToString(), User = "Server"})
                ;
        }


        public void RemoveMsg(string msgType)
        {
            Console.WriteLine("{0} removed", msgType);
        }

        public void AddMsg(string msgType)
        {
            Console.WriteLine("{0} added", msgType);
        }

    }
}