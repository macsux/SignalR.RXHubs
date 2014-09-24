using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public interface IServerHub
    {
        void Send(string name, string message);
        void AddMsg(string msgType);
        void RemoveMsg(string msgType);
    }
}
