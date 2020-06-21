using System;
using System.Runtime.InteropServices;

namespace Cobalt.Common.Native
{
    // TODO try ref structs?
    // https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code 
    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct#ref-struct
    public static class Ffi
    {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct String
        {
            public readonly IntPtr Buffer;
            public readonly IntPtr Capacity;
            public readonly IntPtr Length;

            public override string ToString()
            {
                return Marshal.PtrToStringUni(Buffer, Length.ToInt32());
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Error
        {
            [FieldOffset(0)]
            public readonly long Tag;

            [FieldOffset(8)] public readonly uint Win32Code;
            [FieldOffset(8)] public readonly String CustomCause;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void OnNext(void* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnError(out Error err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnComplete();

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Subscription
        {
            public readonly OnNext OnNext;
            public readonly OnError OnError;
            public readonly OnComplete OnComplete;

            public Subscription(OnNext onNext, OnError onError, OnComplete onComplete)
            {
                OnNext = onNext;
                OnError = onError;
                OnComplete = onComplete;
            }
        }
    }
}
