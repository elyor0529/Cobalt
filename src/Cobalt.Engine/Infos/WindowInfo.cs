using System;
using System.Reactive;
using System.Reactive.Linq;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Utils;
using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public class WindowInfo : IEquatable<WindowInfo>, IDisposable
    {
        private static readonly string ApplicationFrameHost = @"C:\Windows\System32\ApplicationFrameHost.exe";

        public WindowInfo(BasicWindowInfo win)
        {
            Handle = win.Handle;
            Title = win.Title;
            IsWinStoreApp = false;
            ThreadId = User32.GetWindowThreadProcessId(Handle, out var pid);
            using var proc = Kernel32.OpenProcess(
                ACCESS_MASK.FromEnum(Kernel32.ProcessAccess.PROCESS_VM_READ |
                                     Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION), false, pid);

            Path = ProcessInfo.GetPath(proc);

            if (ApplicationFrameHost.Equals(Path, StringComparison.OrdinalIgnoreCase))
            {
                // UWP
                var appWin = HWND.NULL;
                while (appWin == HWND.NULL)
                    User32.EnumChildWindows(Handle, (hdl, id) =>
                    {
                        User32.GetWindowThreadProcessId(hdl, out var cpid);
                        if (cpid == pid) return true;

                        appWin = hdl;
                        return false;
                    }, IntPtr.Zero);
                Handle = appWin;
                ThreadId = User32.GetWindowThreadProcessId(Handle, out pid);
                Path = ProcessInfo.GetPath(proc);
                IsWinStoreApp = true;
            }

            ProcessId = pid;
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

        public bool Equals(WindowInfo other)
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
            return Equals((WindowInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProcessId, ThreadId, Handle, Title);
        }
    }
}