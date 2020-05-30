using System;
using System.Reactive.Linq;
using Cobalt.Engine.Native;
using BasicWindowInfo = Cobalt.Engine.Infos.BasicWindowInfo;
using ForegroundWindowSwitch = Cobalt.Engine.Infos.ForegroundWindowSwitch;

namespace Cobalt.Engine.Watchers
{
    public static class Window
    {
        public static readonly IObservable<ForegroundWindowSwitch> ForegroundWatcher =
            new StaticWatcher<Native.ForegroundWindowSwitch>(
                    Methods.foreground_window_watcher_begin,
                    Methods.foreground_window_watcher_end)
                .Select(x => new ForegroundWindowSwitch(DateTime.FromFileTime(x.FileTimeTicks),
                    new BasicWindowInfo(x.Window.Handle, x.Window.Title.ToString())));
    }
}