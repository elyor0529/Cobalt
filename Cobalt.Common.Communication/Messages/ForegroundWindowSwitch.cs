using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Cobalt.Common.Communication.Messages
{
    [DataContract]
    public class ForegroundWindowSwitch
    {
        [DataMember(Order = 1)]
        public long WindowId { get; set; }
    }
}
