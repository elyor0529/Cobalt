using System;
using System.Runtime.InteropServices;

namespace Cobalt.Engine.Infos
{
    public class ForegroundWindowSwitch
    {
        public ForegroundWindowSwitch(DateTime timestamp, Window window)
        {
            Timestamp = timestamp;
            Window = window;
        }

        public DateTime Timestamp { get; }
        public Window Window { get; }


        [StructLayout(LayoutKind.Sequential)]
        public struct Native
        {
            public readonly Window.Native Window;
            public readonly long FileTimeTicks;
        }
    }
}