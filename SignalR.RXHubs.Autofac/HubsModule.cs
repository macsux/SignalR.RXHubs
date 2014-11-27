using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Practices.ServiceLocation;
using SignalR.RXHubs.Core;
using Module = Autofac.Module;

namespace SignalR.RXHubs.Autofac
{
    public class HubsModule : Module
    {
        private readonly Assembly[] _controllerAssemblies;

        public HubsModule() : this(new[] { Assembly.GetEntryAssembly() })
        {
        }

        public HubsModule(Assembly[] controllerAssemblies)
        {
            _controllerAssemblies = controllerAssemblies;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BufferedRetryDispatch>().As<IObservableDispatch>();
            builder.RegisterType<ContainerHubDescriptorProvider>().AsImplementedInterfaces();

            var virtualHubTypes = _controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => !type.IsAbstract && typeof(IVirtualHub).IsAssignableFrom(type)));
            var normalHubTypes = _controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => typeof(IHub).IsAssignableFrom(type) && !typeof(IVirtualHub).IsAssignableFrom(type)));
            builder.RegisterTypes(normalHubTypes.ToArray());
            
            var hubGenerator = new HubFactoryGenerator();
            foreach (var virtualHubType in virtualHubTypes)
            {
                builder.RegisterType(virtualHubType).AsSelf();
                Type type = virtualHubType;
                var virtualHubFactory = new HubFactory(virtualHubType, () => (IHub) ServiceLocator.Current.GetInstance(type));
                var realHubFactory = hubGenerator.GetRealHubFactory(virtualHubFactory);
                builder.Register(context => (object)realHubFactory.Factory()).As<IHub>().As(realHubFactory.HubType).ExternallyOwned();
            }
        }
    }
}
