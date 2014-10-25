using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Autofac;
using Autofac.Core;
using Autofac.Integration.SignalR;
using Castle.DynamicProxy;
using Contract;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Cors;
using Owin;

namespace SignalRSelfHost
{
    internal class Startup
    {
        private class EmptyInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {

            }
        }

        public void Configuration(IAppBuilder app)
        {
            var generator = new ProxyGenerator();
            var realHubInterfaceType = ObservableHub<IServerHub>.GeneratePrivateHubTypeForInterface(generator);

            
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<AutofacHubDescriptorProvider>().AsImplementedInterfaces();
            containerBuilder.RegisterType<MyHub>().As<IVirtualHub>();

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
}