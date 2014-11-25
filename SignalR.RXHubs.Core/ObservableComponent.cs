using System.Runtime.Serialization;

namespace SignalR.RXHubs.Core
{
    public enum ObservableComponent
    {
        [EnumMember(Value = "N")]
        Next,
        [EnumMember(Value = "E")]
        Error,
        [EnumMember(Value = "C")]
        Complete
    }
}