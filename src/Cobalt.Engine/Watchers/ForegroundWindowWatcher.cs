using System;
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

        private static readonly string ApplicationFrameHost = @"C:\Windows\System32\ApplicationFrameHost.exe";

        private void ForegroundWindowChanged(User32.HWINEVENTHOOK hwineventhook, uint winevent, HWND hwnd, int idobject,
            int idchild, uint ideventthread, uint dwmseventtime)
        {
            var dwmsTimestamp = GetDwmsTimestamp(dwmseventtime);
            if (!User32.IsWindow(hwnd) || !User32.IsWindowVisible(hwnd) || User32.IsIconic(hwnd)) return;

            var tid = User32.GetWindowThreadProcessId(hwnd, out var pid);
            var proc = Kernel32.OpenProcess(
                ACCESS_MASK.FromEnum(Kernel32.ProcessAccess.PROCESS_VM_READ |
                                     Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION), false, pid);

            var path = ProcessInfo.GetPath(proc);
            var isWinStoreApp = false;

            if (path.Equals(ApplicationFrameHost, StringComparison.OrdinalIgnoreCase))
            {
                // UWP
                var appWin = HWND.NULL;
                User32.EnumChildWindows(hwnd, (hdl, id) =>
                {
                    User32.GetWindowThreadProcessId(hdl, out var cpid);
                    if (cpid == pid) return true;
                    
                    appWin = hdl;
                    return false;
                }, IntPtr.Zero);
                hwnd = appWin;
                tid = User32.GetWindowThreadProcessId(hwnd, out pid);
                path = ProcessInfo.GetPath(proc);
                isWinStoreApp = true;
            }

            var title = "";

            try
            {

                title = GetTitle(hwnd);
            }
            catch (Exception e)
            {

            }

            proc.Dispose();
            Events.OnNext(new ForegroundWindowSwitch(dwmsTimestamp,
                new BasicWindowInfo(pid, tid, hwnd, title, path, isWinStoreApp)));
        }

        private string GetTitle(HWND hwnd)
        {
            var length = User32.GetWindowTextLength(hwnd);
            if (length == 0) Win32Error.ThrowLastError();
            var title = new StringBuilder(length);
            User32.GetWindowText(hwnd, title, length + 1);
            return title.ToString();
        }

        private DateTime GetDwmsTimestamp(uint dwmseventtime)
        {
            return DateTime.Now.AddMilliseconds(dwmseventtime - Environment.TickCount);
        }
    }
}