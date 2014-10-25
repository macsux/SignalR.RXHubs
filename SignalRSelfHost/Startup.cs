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
        public void Configuration(IAppBuilder app)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterAssemblyModules(typeof(Startup).Assembly);
            var container = containerBuilder.Build();

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