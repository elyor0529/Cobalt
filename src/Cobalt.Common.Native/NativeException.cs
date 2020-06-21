using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Cobalt.Common.Native
{
    public sealed class NativeException : Exception
    {
        public static NativeException From(Ffi.Error err)
        {
            return err.Tag == 0 ?
                new NativeException((int)err.Win32Code) :
                new NativeException(err.CustomCause.ToString());
        }

        public NativeException(string cause) : base(cause)
        {
            Source = "Custom";
        }

        public NativeException(int code) : base(null, new Win32Exception(code))
        {
            Source = "Win32";
        }
    }
}
