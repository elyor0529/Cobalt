using System;
using System.Reactive;
using System.Reactive.Linq;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Utils;
using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public class BasicWindowInfo : IEquatable<BasicWindowInfo>, IDisposable
    {
        public BasicWindowInfo(uint pid, uint tid, HWND hWnd, string title, string path, bool isWinStoreApp)
        {
            ProcessId = pid;
            ThreadId = tid;
            Handle = hWnd;
            Title = title;
            Path = path;
            IsWinStoreApp = isWinStoreApp;
        }

        public uint ProcessId { get; }
        public uint ThreadId { get; }
        public HWND Handle { get; }
        public string Title { get; }
        public string Path { get; }
        public bool IsWinStoreApp { get; }
        public Session Session { get; set; }


        public IObservable<Unit> Closed =>
            Observable.Create<Unit>(obs =>
            {
                User32.WinEventProc windowClosed = (eventHook, evnt, hwnd, idObject, child, thread, time) =>
                {
                    if (idObject != User32.ObjectIdentifiers.OBJID_WINDOW || child != 0 || hwnd != Handle) return;
                    obs.OnNext(Unit.Default);
                    obs.OnCompleted();
                    User32.UnhookWinEvent(eventHook).CheckValid();
                };
                User32.SetWinEventHook(
                    User32.EventConstants.EVENT_OBJECT_DESTROY,
                    User32.EventConstants.EVENT_OBJECT_DESTROY,
                    HINSTANCE.NULL, windowClosed, ProcessId, ThreadId,
                    User32.WINEVENT.WINEVENT_OUTOFCONTEXT).CheckValid();
                return () => { windowClosed = null; };
            });

        public void Dispose()
        {
        }

        public bool Equals(BasicWindowInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ProcessId == other.ProcessId && ThreadId == other.ThreadId && Handle.Equals(other.Handle) &&
                   Title == other.Title;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BasicWindowInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProcessId, ThreadId, Handle, Title);
        }
    }
}