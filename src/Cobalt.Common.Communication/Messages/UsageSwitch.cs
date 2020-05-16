using System.Runtime.Serialization;

namespace Cobalt.Common.Communication.Messages
{
    [DataContract]
    public class UsageSwitch
    {
        [DataMember(Order = 1)] public long UsageId { get; set; }
        [DataMember(Order = 2)] public long SessionId { get; set; }
        [DataMember(Order = 3)] public long AppId { get; set; }
        [DataMember(Order = 4)] public long NewSessionId { get; set; }
        [DataMember(Order = 5)] public long NewAppId { get; set; }
    }
}