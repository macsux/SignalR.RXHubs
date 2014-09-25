using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public interface IServerHub : IHubSupportsObservables
    {
        void Send(ClientMessage message);
        Guid MsgSubscribe();
        
    }

    public interface IHubSupportsObservables
    {
        void Unsubscribe(Guid observableId);
    }
}
