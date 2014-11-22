using System;
using Newtonsoft.Json.Linq;

namespace SignalR.RXHubs.Core
{
    public class ObservableNotification
    {
        public ObservableNotification(Guid id, JToken payload)
        {
            this.SubscriptionId = id;
            this.Message = payload;
        }
        public Guid SubscriptionId { get; set; }
        public int SequenceNo { get; set; }
        public JToken Message { get; set; }
    }
}