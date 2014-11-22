using System;

namespace SignalR.RXHubs
{
    public class MessageBuffer
    {
        public MessageBuffer(object payload)
        {
            Payload = payload;
        }

        public DateTime LastSentAttempt { get; set; }
        public object Payload { get; set; }
    }
}