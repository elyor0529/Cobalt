using System;
using System.Runtime.InteropServices;

namespace Cobalt.Engine.Native
{
    public static partial class Methods
    {
        [DllImport(Constants.NativeLibrary)]
        public static extern void foreground_window_watcher_begin(Subscription sub);

        [DllImport(Constants.NativeLibrary)]
        public static extern void foreground_window_watcher_end();

        [DllImport(Constants.NativeLibrary)]
        public static extern IntPtr window_closed_begin(Subscription sub, IntPtr handle);

        [DllImport(Constants.NativeLibrary)]
        public static extern IntPtr process_exit_begin(Subscription sub, IntPtr handle);

        [DllImport(Constants.NativeLibrary)]
        public static extern void event_loop_step();
    }
}