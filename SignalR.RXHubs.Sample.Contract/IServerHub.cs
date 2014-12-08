using System;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs.Sample.Contract
{
    public interface IServerHub : IVirtualHub
    {
        void Send(ClientMessage message);
        IObservable<ClientMessage> GetClientMessageObservable();
    }
}
