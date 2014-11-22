using Autofac;
using Autofac.Extras.CommonServiceLocator;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Practices.ServiceLocation;
using Owin;
using SignalR.RXHubs.Autofac;

namespace SignalR.RXHubs.Sample.Server
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<HubsModule>();
            containerBuilder.Register(c =>
            {
                var context = c.Resolve<IComponentContext>();
                var genericContainer = new AutofacServiceLocator(context);
                return genericContainer;
            }).As<IServiceLocator>().SingleInstance();
            var container = containerBuilder.Build();
            var csl = new AutofacServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => csl);

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