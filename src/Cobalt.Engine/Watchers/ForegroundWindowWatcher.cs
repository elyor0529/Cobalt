using System;
using Cobalt.Common.Utils;
using Vanara.PInvoke;

namespace Cobalt.Engine.Watchers
{
    public class ForegroundWindowWatcher : Watcher<WindowInfo>
    {
        private User32.WinEventProc _foregroundWindowChanged;
        private User32.HWINEVENTHOOK _hook;

        public ForegroundWindowWatcher()
        {
            _foregroundWindowChanged = ForegroundWindowChanged;
        }

        public override void Dispose()
        {
            User32.UnhookWinEvent(_hook).CheckValid();
            _hook = User32.HWINEVENTHOOK.NULL;
            _foregroundWindowChanged = null;
        }

        public override void Watch()
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
            var dwmsTimestamp = GetDwmsTimestamp(dwmseventtime);
            if (!User32.IsWindowVisible(hwnd)) return;
            Events.OnNext(new WindowInfo {ActivatedTimestamp = dwmsTimestamp});
        }

        private DateTimeOffset GetDwmsTimestamp(uint dwmseventtime)
        {
            return DateTimeOffset.Now.AddMilliseconds(dwmseventtime - Environment.TickCount);
        }
    }
}