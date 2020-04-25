using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Cobalt.Common.Utils
{
    public static class Extensions
    {
        public static void CheckValid(this bool b)
        {
            if (!b) return;
            throw new Win32Exception(Marshal.GetLastWin32Error()); 
        }

        public static T CheckValid<T>(this T p)
        {
            if (p.GetHashCode() != IntPtr.Zero.GetHashCode()) return p;
            throw new Win32Exception(Marshal.GetLastWin32Error()); 
        }
    }
}
