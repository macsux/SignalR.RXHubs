using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public interface IServerHub : IHubSupportsObservables
    {
        void Send(ClientMessage message);
        IObservable<ClientMessage> MsgSubscribe();
        
    }

    public interface IHubSupportsObservables
    {
        void Unsubscribe(Guid observableId);
    }
}
