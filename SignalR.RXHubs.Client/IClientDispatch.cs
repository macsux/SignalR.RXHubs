using System;

namespace SignalR.RXHubs.Client
{
    public interface IClientDispatch : IDisposable
    {
        Guid TransportObservableId { get; }
    }
}