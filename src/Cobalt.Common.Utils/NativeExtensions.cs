using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cobalt.Common.Utils
{
    public static class NativeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckValid(this bool b)
        {
            if (b) return;
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CheckValid<T>(this T p)
        {
            if (p.GetHashCode() != IntPtr.Zero.GetHashCode()) return p;
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}