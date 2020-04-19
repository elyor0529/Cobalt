using System;

namespace Cobalt.Common.Data.Entities
{
    public enum SystemEventType
    {
        Start,
        Shutdown,
        Login,
        Logoff,
        InteractionStop,
        InteractionResume
    }

    public class SystemEvent : Entity
    {
        public DateTimeOffset Timestamp { get; set; }
        public SystemEventType Type { get; set; }
    }
}