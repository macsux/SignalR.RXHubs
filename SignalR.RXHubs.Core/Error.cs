using System;
using Newtonsoft.Json;

namespace SignalR.RXHubs.Core
{
    [Serializable]
    public class Error : SequenceEnd
    {
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }

        [JsonConstructor]
        public Error(string message)
            //: base(totalMessages)
        {
            Message = message;
            TimeStamp = DateTime.Now;
        }

        public Error(Exception ex)
            : this(ex.Message)
        {
            StackTrace = ex.StackTrace;
        }

        public override string ToString()
        {
            return Message + StackTrace;
        }
    }

    [Serializable]
    public class SequenceEnd
    {
    }
}