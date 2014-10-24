using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json.Serialization;

namespace SignalRSelfHost
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
            var hubs = _container.Resolve<IEnumerable<IHub>>();
            var retval = hubs.Select(x => new HubDescriptor()
            {
                HubType = x.GetType(),
                NameSpecified = false,
                Name = x.GetType().Name
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
