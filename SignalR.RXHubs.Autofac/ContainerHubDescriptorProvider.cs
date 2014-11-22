using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Practices.ServiceLocation;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs.Autofac
{
    public class ContainerHubDescriptorProvider : IHubDescriptorProvider
    {
        private readonly IServiceLocator _container;

        public ContainerHubDescriptorProvider(IServiceLocator container)
        {
            _container = container;
        }

        public IList<HubDescriptor> GetHubs()
        {
            var hubs = _container.GetAllInstances<IHub>().Where(hub => !(hub is IVirtualHub));
            var retval = hubs.Select(hub => new HubDescriptor()
            {
                HubType = hub.GetType(),
                NameSpecified = (hub.GetType().GetHubAttributeName() != null),
                Name = hub.GetType().GetHubName()
            }).ToList();
            return retval;
        }

        public bool TryGetHub(string hubName, out HubDescriptor descriptor)
        {
            descriptor = GetHubs().FirstOrDefault(x => x.Name == hubName);
            return descriptor != null;
        }

    }
}