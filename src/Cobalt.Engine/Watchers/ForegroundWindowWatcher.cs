using System;
using System.Diagnostics;
using System.Text;
using Cobalt.Common.Utils;
using Cobalt.Engine.Infos;
using Vanara.PInvoke;

namespace Cobalt.Engine.Watchers
{
    public class ForegroundWindowWatcher : Watcher<ForegroundWindowSwitch>
    {
        private User32.WinEventProc _foregroundWindowChanged;
        private User32.HWINEVENTHOOK _hook;

        public ForegroundWindowWatcher()
        {
            _foregroundWindowChanged = ForegroundWindowChanged;
        }

        public override void Dispose()
        {
            Debug.WriteLine("E"+Kernel32.GetCurrentThreadId());
            User32.UnhookWinEvent(_hook).CheckValid();
            _hook = User32.HWINEVENTHOOK.NULL;
            _foregroundWindowChanged = null;
        }

        public override void Watch()
        {
            Debug.WriteLine("S"+Kernel32.GetCurrentThreadId());
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
            if (!User32.IsWindow(hwnd) || !User32.IsWindowVisible(hwnd) || User32.IsIconic(hwnd)) return;
            var tid = User32.GetWindowThreadProcessId(hwnd, out var pid);
            var length = User32.GetWindowTextLength(hwnd);
            if (length == 0) return;
            var title = new StringBuilder(length);
            User32.GetWindowText(hwnd, title, length + 1);
            Events.OnNext(new ForegroundWindowSwitch(dwmsTimestamp, new BasicWindowInfo(pid, tid, hwnd, title.ToString())));
        }

        private DateTime GetDwmsTimestamp(uint dwmseventtime)
        {
            return DateTime.Now.AddMilliseconds(dwmseventtime - Environment.TickCount);
        }
    }
}