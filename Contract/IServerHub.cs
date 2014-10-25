using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public interface IServerHub : IVirtualHub //IHubSupportsObservables
    {
        void Send(ClientMessage message);
        IObservable<ClientMessage> GetClientMessageObservable();
        
    }

    
}
