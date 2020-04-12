using System;
using System.Collections.Generic;
using System.Text;

namespace Cobalt.Common.Data.Entities
{
    public enum AppUsageType
    {
        Foreground,
        InView
    }

    public class AppUsage : Entity
    {
        public App App { get; set; }
        public DateTimeOffset StartTimestamp { get; set; }
        public DateTimeOffset EndTimestamp { get; set; }
        public AppUsageType Type { get; set; }
    }
}
