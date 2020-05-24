using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Cobalt.Engine.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeObservable<T> where T: struct
    {
        public Action<T> OnNext;
        public Action<uint> OnError;
        public Action OnCompleted;
    }

    public static class Watchers
    {
        [DllImport(Constants.NativeLibrary)]
        public static extern int add(int a, int b);

        [DllImport(Constants.NativeLibrary)]
        public static extern void interval(NativeObservable<uint> obs);
    }
}
