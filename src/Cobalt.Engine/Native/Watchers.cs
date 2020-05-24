using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Cobalt.Engine.Native
{
    public static class Watchers
    {
        [DllImport("native")]
        public static extern int add(int a, int b);
    }
}
