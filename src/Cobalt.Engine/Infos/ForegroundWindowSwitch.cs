using System;
using System.Collections.Generic;
using System.Text;

namespace Cobalt.Engine.Infos
{
    public class ForegroundWindowSwitch
    {
        public ForegroundWindowSwitch(DateTime activated, BasicWindowInfo window)
        {
            ActivatedTimestamp = activated;
            Window = window;
        }

        public DateTime ActivatedTimestamp { get;  }
        public BasicWindowInfo Window { get; }
    }
}
