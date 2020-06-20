using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cobalt.Engine.Native
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void OnNext(void* arg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnCompleted();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnError(Error err);

    [StructLayout(LayoutKind.Sequential)]
    public struct FfiString
    {
        public readonly IntPtr Pointer;
        public readonly IntPtr Capacity;
        public readonly IntPtr Length;

        public override string ToString()
        {
            return Length == IntPtr.Zero ? null : Marshal.PtrToStringUni(Pointer, Length.ToInt32());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Error
    {
        public uint Code;
        public IntPtr Cause;

        public Exception ToException()
        {
            return new Exception($"[{Code}]: {Marshal.PtrToStringUni(Cause)}");
        }
    }

    // TODO doesn't work cuz generics don't work in PInvoke
    [StructLayout(LayoutKind.Explicit)]
    public struct FfiResult
    {
        [FieldOffset(0)] public long Tag;

        [FieldOffset(8)] public uint Ok;
        [FieldOffset(8)] public Error Error;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FfiOption<T> where T: unmanaged
    {
        public long Tag;
        public T Value;
    }

    public static class What
    {
        [DllImport(Constants.NativeLibrary)]
        public static extern FfiResult add();

        [DllImport(Constants.NativeLibrary)]
        public static extern FfiString uwp_aumid(IntPtr handle);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Subscription
    {
        public OnNext OnNext;
        public OnError OnError;
        public OnCompleted OnCompleted;
    }

    public delegate void StaticWatch(Subscription sub);

    public delegate void StaticDrop();

    public delegate IntPtr Watch(Subscription sub);

    public delegate void Drop(IntPtr ptr);

    public class StaticWatcher<T> : IObservable<T> where T : unmanaged
    {
        private readonly StaticDrop _drop;
        private readonly StaticWatch _watch;

        public StaticWatcher(StaticWatch watch, StaticDrop drop)
        {
            _watch = watch;
            _drop = drop;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return new StaticWatcherSubscription<T>(_watch, _drop, observer);
        }
    }

    public unsafe class StaticWatcherSubscription<T> : IDisposable where T : unmanaged
    {
        private readonly StaticDrop _drop;
        private readonly IObserver<T> _observer;
        private readonly OnCompleted _onCompleted;
        private readonly OnError _onError;
        private readonly OnNext _onNext;
        private readonly Subscription _subscription;

        public StaticWatcherSubscription(StaticWatch watch, StaticDrop drop, IObserver<T> observer)
        {
            _onNext = OnNext;
            _onError = OnError;
            _onCompleted = OnCompleted;
            _subscription = new Subscription {OnNext = _onNext, OnError = _onError, OnCompleted = _onCompleted};
            _observer = observer;
            _drop = drop;
            watch(_subscription);
        }

        public void Dispose()
        {
            _drop();
        }

        public void OnNext(void* ptr)
        {
            _observer.OnNext(*(T*)ptr);
        }

        public void OnError(Error err)
        {
            _observer.OnError(err.ToException());
        }

        public void OnCompleted()
        {
            _observer.OnCompleted();
        }
    }

    public class Watcher<T> : IObservable<T> where T : unmanaged
    {
        private readonly Drop _drop;
        private readonly Watch _watch;

        public Watcher(Watch watch, Drop drop)
        {
            _watch = watch;
            _drop = drop;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return new WatcherSubscription<T>(_watch, _drop, observer);
        }
    }

    public unsafe class WatcherSubscription<T> : IDisposable where T : unmanaged
    {
        private readonly Drop _drop;
        private readonly IObserver<T> _observer;
        private readonly OnCompleted _onCompleted;
        private readonly OnError _onError;
        private readonly OnNext _onNext;
        private readonly IntPtr _ptr;
        private readonly Subscription _subscription;

        public WatcherSubscription(Watch watch, Drop drop, IObserver<T> observer)
        {
            _onNext = OnNext;
            _onError = OnError;
            _onCompleted = OnCompleted;
            _subscription = new Subscription {OnNext = _onNext, OnError = _onError, OnCompleted = _onCompleted};
            _observer = observer;
            _drop = drop;
            _ptr = watch(_subscription);
        }

        public void Dispose()
        {
            _drop(_ptr);
        }

        public void OnNext(void* ptr)
        {
            _observer.OnNext(*(T*)ptr);
        }

        public void OnError(Error err)
        {
            _observer.OnError(err.ToException());
        }

        public void OnCompleted()
        {
            _observer.OnCompleted();
        }
    }
}