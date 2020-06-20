using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Cobalt.Engine.Native;

namespace Cobalt.Engine.Infos
{
    public class Window : IDisposable
    {
        public static readonly IObservable<ForegroundWindowSwitch> ForegroundWatcher =
            new StaticWatcher<ForegroundWindowSwitch.Native>(
                    Methods.foreground_window_watcher_begin,
                    Methods.foreground_window_watcher_end)
                .Select(x => new ForegroundWindowSwitch(DateTime.FromFileTime(x.FileTimeTicks),
                    new Window(x.Window.Handle, x.Window.Title.ToString())));
        public static IObservable<Unit> ClosedWatcher(IntPtr handle) =>
            new Watcher<Unit>(sub => Methods.window_closed_begin(sub, handle), x => { });

        private Window(IntPtr handle, string title)
        {
            Handle = handle;
            Title = title;
        }

        public IntPtr Handle { get; }
        public string Title { get; }
        public IObservable<Unit> Closed => ClosedWatcher(Handle);

        public Process Process => new Process(this);

        [StructLayout(LayoutKind.Sequential)]
        public struct Native
        {
            public readonly IntPtr Handle;
            public readonly FfiString Title;
        }

        public void Dispose()
        {
        }
    }
}