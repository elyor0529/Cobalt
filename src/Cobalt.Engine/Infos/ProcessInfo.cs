using System;
using System.Reactive;
using System.Reactive.Linq;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Utils;
using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public class ProcessInfo : IEquatable<ProcessInfo>, IDisposable
    {
        public ProcessInfo(uint pid)
        {
            Id = pid;
            Handle =
                Kernel32.OpenProcess(
                        ACCESS_MASK.FromEnum(Kernel32.ProcessAccess.PROCESS_VM_READ) | ACCESS_MASK.SYNCHRONIZE,
                        false,
                        pid)
                    .CheckValid();
        }

        public uint Id { get; }
        public Kernel32.SafeHPROCESS Handle { get; set; }

        public AppIdentification Identification { get; } // generate

        public IObservable<Unit> Exited
            => Observable.Create<Unit>(obs =>
            {
                Kernel32.SafeRegisteredWaitHandle wait = null;
                Kernel32.WaitOrTimerCallback callback = (ctx, fired) =>
                {
                    obs.OnNext(Unit.Default);
                    obs.OnCompleted();
                    wait?.Dispose();
                };
                Kernel32.RegisterWaitForSingleObject(out wait, Handle, callback, IntPtr.Zero,
                    Kernel32.INFINITE,
                    Kernel32.WT.WT_EXECUTEONLYONCE).CheckValid();
                wait.CompletionEvent = Kernel32.SafeEventHandle.InvalidHandle;

                return () => { callback = null; };
            });

        public void Dispose()
        {
            Handle.Dispose();
        }


        public bool Equals(ProcessInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProcessInfo) obj);
        }

        public override int GetHashCode()
        {
            return (int) Id;
        }
    }
}