using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.RXHubs
{
    public class HubFactory
    {
        public HubFactory(Type hubType, Func<IHub> factory)
        {
            this.Factory = factory;
            this.HubType = hubType;
        }
        public Func<IHub> Factory { get; private set; }
        public Type HubType { get; private set; }
    }
}
