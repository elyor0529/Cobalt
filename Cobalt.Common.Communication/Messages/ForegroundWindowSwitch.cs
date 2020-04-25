using System.Runtime.Serialization;

namespace Cobalt.Common.Communication.Messages
{
    [DataContract]
    public class ForegroundWindowSwitch
    {
        [DataMember(Order = 1)] public long WindowId { get; set; }
    }
}