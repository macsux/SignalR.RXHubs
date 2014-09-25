using System;
using Contract;

namespace Client
{
    public class RemoteException : Exception
    {
        public string RemoteStackTrace { get; set; }
        public RemoteException(Error error) : base(error.Message)
        {
            RemoteStackTrace = error.StackTrace;
        }
    }
}