using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using Cobalt.Engine.Native;

namespace Cobalt.Engine.Watchers
{
    public static class Process
    {
        public static IObservable<Unit> Exited(IntPtr handle) =>
            new Native.Watcher<Unit>(x => Methods.process_exit_begin(x, handle), x => { });
    }
}
