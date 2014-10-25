using System;

namespace Contract
{
    public interface IHubSupportsObservables
    {
        void Unsubscribe(Guid observableId);
    }
}