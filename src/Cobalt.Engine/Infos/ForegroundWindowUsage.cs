using System;
using System.Collections.Generic;
using System.Text;

namespace Cobalt.Engine.Infos
{
    public class ForegroundWindowUsage
    {
        public ForegroundWindowUsage(ForegroundWindowSwitch s1, ForegroundWindowSwitch s2)
        {
            Start = s1.ActivatedTimestamp;
            End = s2.ActivatedTimestamp;
            CurrentWindow = s1.Window;
            NewWindow = s2.Window;
        }

        public DateTime Start { get; }
        public DateTime End { get; }
        public BasicWindowInfo CurrentWindow { get; }
        public BasicWindowInfo NewWindow { get; }
    }
}
