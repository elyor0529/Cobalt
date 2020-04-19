using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ProtoBuf;

namespace Cobalt.Common.Transmission.Messages
{
    [ProtoContract]
    public class AppSwitchMessage
    {
        [ProtoMember(1)]
        public string AppName { get; set; }


        [ProtoMember(2)]
        public string AppDescription { get; set; }

        [ProtoMember(3)]
        public string AppCommandLine { get; set; }
    }

    [ProtoContract]
    public class AppSwitchRequest
    {
        [ProtoMember(1)]
        public string AppName { get; set; }
    }
}
