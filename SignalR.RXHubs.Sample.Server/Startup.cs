using Autofac;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;

namespace SignalR.RXHubs.Sample.Server
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<HubsModule>();
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