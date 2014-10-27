using System;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs.Sample.Contract
{
    public interface IServerHub : IVirtualHub //IHubSupportsObservables
    {
        void Send(ClientMessage message);
        IObservable<ClientMessage> GetClientMessageObservable();
        
    }

    
}
