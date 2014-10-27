using System;

namespace SignalR.RXHubs.Core
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