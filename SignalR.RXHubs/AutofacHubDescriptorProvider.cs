using System.Collections.Generic;
using System.Linq;
using Autofac;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs
{
    public class AutofacHubDescriptorProvider : IHubDescriptorProvider
    {
        private IComponentContext _container;

        public AutofacHubDescriptorProvider(IComponentContext container)
        {
            _container = container;
        }

        public IList<HubDescriptor> GetHubs()
        {
            var hubs = _container.Resolve<IEnumerable<IHub>>().Where(hub => !(hub is IVirtualHub));
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
