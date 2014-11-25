using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.RXHubs.Core
{
    public class ObservableNotification
    {
        
        public ObservableNotification(Guid id, long msgNumber, ObservableComponent component, JToken payload)
        {
            this.SubscriptionId = id;
            MsgNumber = msgNumber;
            Component = component;
            this.Message = payload;
        }
        [JsonProperty("I")]
        public Guid SubscriptionId { get; set; }
        [JsonProperty("N")]
        public long MsgNumber { get; set; }
        [JsonProperty("C")]
        public ObservableComponent Component { get; set; }
        [JsonProperty("M")]
        public JToken Message { get; set; }
    }
}