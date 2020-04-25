using System;
using System.Reactive.Subjects;
using Cobalt.Common.Utils;
using Vanara.PInvoke;

namespace Cobalt.Engine
{
    public class ForegroundWindowWatcher : IDisposable
    {
        private readonly Subject<(HWND, DateTimeOffset)> _newWindows;
        private User32.WinEventProc _foregroundWindowChanged;
        private User32.HWINEVENTHOOK _hook;

        public ForegroundWindowWatcher()
        {
            _newWindows = new Subject<(HWND, DateTimeOffset)>();
            _foregroundWindowChanged = ForegroundWindowChanged;
        }

        public IObservable<(HWND, DateTimeOffset)> WindowChanged => _newWindows;

        public void Dispose()
        {
            User32.UnhookWinEvent(_hook).CheckValid();
            _hook = User32.HWINEVENTHOOK.NULL;
            _foregroundWindowChanged = null;
        }

        public void Watch()
        {
            _hook = User32.SetWinEventHook(
                User32.EventConstants.EVENT_SYSTEM_FOREGROUND,
                User32.EventConstants.EVENT_SYSTEM_FOREGROUND,
                HINSTANCE.NULL, _foregroundWindowChanged, 0, 0,
                User32.WINEVENT.WINEVENT_OUTOFCONTEXT).CheckValid();
        }

        private void ForegroundWindowChanged(User32.HWINEVENTHOOK hwineventhook, uint winevent, HWND hwnd, int idobject,
            int idchild, uint ideventthread, uint dwmseventtime)
        {
            var dwmsTimestamp = DateTimeOffset.Now.AddMilliseconds(dwmseventtime - Environment.TickCount);
            _newWindows.OnNext((hwnd, dwmsTimestamp));
        }
    }
}