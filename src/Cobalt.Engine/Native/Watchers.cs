using System;
using System.Runtime.InteropServices;

namespace Cobalt.Engine.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BasicWindowInfo
    {
        public readonly IntPtr Handle;
        public readonly FfiString Title;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ForegroundWindowSwitch
    {
        public readonly BasicWindowInfo Window;
        public readonly long FileTimeTicks;
    }

    public static partial class Methods
    {
        [DllImport(Constants.NativeLibrary)]
        public static extern void foreground_window_watcher_begin(Subscription sub);

        [DllImport(Constants.NativeLibrary)]
        public static extern void foreground_window_watcher_end();

        [DllImport(Constants.NativeLibrary)]
        public static extern void event_loop_step();
    }
}