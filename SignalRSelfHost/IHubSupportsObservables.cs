using System;

namespace SignalRSelfHost
{
    public interface IHubSupportsObservables
    {
        void Unsubscribe(Guid observableId);
    }
}