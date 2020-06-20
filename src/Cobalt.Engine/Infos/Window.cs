﻿using System;
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
                    new Window(x.Window)));

        [DllImport(Constants.NativeLibrary)]
        private static extern Extended window_extended(Basic basic);

        public static IObservable<Unit> ClosedWatcher(IntPtr handle) =>
            new Watcher<Unit>(sub => Methods.window_closed_begin(sub, handle), x => { });

        private Window(Basic basic)
        {
            _basic = basic;
            _extended = new Lazy<Extended>(() => window_extended(basic));
        }

        private readonly Basic _basic;
        private readonly Lazy<Extended> _extended;

        public IntPtr Handle => _basic.Handle;
        public string Title => _basic.Title.ToString();
        public string UwpAumid => _extended.Value.UwpAumid.ToString();

        public IObservable<Unit> Closed => ClosedWatcher(Handle);
        public Process Process => new Process(this);

        [StructLayout(LayoutKind.Sequential)]
        public struct Basic
        {
            public readonly IntPtr Handle;
            public readonly FfiString Title;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Extended
        {
            public readonly IntPtr Handle;
            public readonly FfiString Title;
            public readonly FfiString UwpAumid;
        }

        public void Dispose()
        {
        }
    }
}