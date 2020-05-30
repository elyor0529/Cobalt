using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cobalt.Engine.Native
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void OnNext(void* arg);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnCompleted();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnError(uint err);

    [StructLayout(LayoutKind.Sequential)]
    public struct FfiString
    {
        public readonly IntPtr Pointer;
        public readonly IntPtr Capacity;
        public readonly IntPtr Length;

        public override string ToString() => Marshal.PtrToStringUni(Pointer, Length.ToInt32()-1);
    }

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
        public readonly long Ticks;
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

    public class StaticWatcher<T> : IObservable<T> where T : struct
    {
        private readonly StaticWatch _watch;
        private readonly StaticDrop _drop;

        public StaticWatcher(StaticWatch watch, StaticDrop drop)
        {
            _watch = watch;
            _drop = drop;
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            new StaticWatcherSubscription<T>(_watch, _drop, observer);
    }
    public unsafe class StaticWatcherSubscription<T> : IDisposable where T: struct
    {
        private readonly Subscription _subscription;
        private readonly OnNext _onNext;
        private readonly OnError _onError;
        private readonly OnCompleted _onCompleted;
        private readonly IObserver<T> _observer;
        private readonly StaticDrop _drop;

        public StaticWatcherSubscription(StaticWatch watch, StaticDrop drop, IObserver<T> observer)
        {
            _onNext = OnNext;
            _onError = OnError;
            _onCompleted = OnCompleted;
            _subscription = new Subscription { OnNext = _onNext, OnError = _onError, OnCompleted = _onCompleted };
            _observer = observer;
            _drop = drop;
            watch(_subscription);
        }

        public void OnNext(void* ptr) =>_observer.OnNext(Unsafe.ReadUnaligned<T>(ptr));
        public void OnError(uint err) =>_observer.OnError(new Exception(err.ToString()));
        public void OnCompleted() => _observer.OnCompleted();

        public void Dispose() => _drop();
    }

    public class Watcher<T> : IObservable<T> where T: struct
    {
        private readonly Watch _watch;
        private readonly Drop _drop;

        public Watcher(Watch watch, Drop drop)
        {
            _watch = watch;
            _drop = drop;
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            new WatcherSubscription<T>(_watch, _drop, observer);
    }

    public unsafe class WatcherSubscription<T> : IDisposable where T: struct
    {
        private readonly Subscription _subscription;
        private readonly OnNext _onNext;
        private readonly OnError _onError;
        private readonly OnCompleted _onCompleted;
        private readonly IObserver<T> _observer;
        private readonly IntPtr _ptr;
        private readonly Drop _drop;

        public WatcherSubscription(Watch watch, Drop drop, IObserver<T> observer)
        {
            _onNext = OnNext;
            _onError = OnError;
            _onCompleted = OnCompleted;
            _subscription = new Subscription { OnNext = _onNext, OnError = _onError, OnCompleted = _onCompleted };
            _observer = observer;
            _drop = drop;
            _ptr = watch(_subscription);
        }

        public void OnNext(void* ptr) => _observer.OnNext(Unsafe.ReadUnaligned<T>(ptr));
        public void OnError(uint err) =>_observer.OnError(new Exception(err.ToString()));
        public void OnCompleted() => _observer.OnCompleted();

        public void Dispose() => _drop(_ptr);
    }

    public static class Watchers
    {
        [DllImport(Constants.NativeLibrary)]
        public static extern int add(int a, int b);

        [DllImport(Constants.NativeLibrary)]
        public static extern void foreground_window_watcher_begin(Subscription sub);

        [DllImport(Constants.NativeLibrary)]
        public static extern void foreground_window_watcher_end();

        [DllImport(Constants.NativeLibrary)]
        public static extern void event_loop();

        public static StaticWatcher<ForegroundWindowSwitch> ForegroundWindowWatcher =>
            new StaticWatcher<ForegroundWindowSwitch>(foreground_window_watcher_begin, foreground_window_watcher_end);
    }
}
