using System;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.RXHubs
{
    public class HubFactory
    {
        public HubFactory(Type hubType, Func<IHub> factory)
        {
            Factory = factory;
            HubType = hubType;
        }
        public Func<IHub> Factory { get; private set; }
        public Type HubType { get; private set; }
    }
}
