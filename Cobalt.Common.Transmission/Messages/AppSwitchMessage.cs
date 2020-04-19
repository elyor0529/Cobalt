using System.Runtime.Serialization;

namespace Cobalt.Common.Transmission.Messages
{
    [DataContract]
    public class AppSwitchMessage
    {
        [DataMember(Order = 1)] public string AppName { get; set; }

        [DataMember(Order = 2)] public string AppDescription { get; set; }

        [DataMember(Order = 3)] public string AppCommandLine { get; set; }
    }
}