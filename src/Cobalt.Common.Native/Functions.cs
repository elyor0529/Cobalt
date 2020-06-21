using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cobalt.Common.Native
{
    public static class Functions
    {
        [DllImport(Constants.NativeLibrary)]
        public static extern void range(uint start, uint end, in Ffi.Subscription sub);
    }
}
