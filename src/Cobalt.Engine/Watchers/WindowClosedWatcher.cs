using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using Cobalt.Common.Utils;
using Cobalt.Engine.Infos;
using Vanara.PInvoke;

namespace Cobalt.Engine.Watchers
{
    public class WindowClosedWatcher : Watcher<HWND>
    {
        private User32.WinEventProc _objectDestroyed;
        private User32.HWINEVENTHOOK _hook;

        public WindowClosedWatcher()
        {
            _objectDestroyed = ObjectDestroyed;
        }

        public override void Dispose()
        {
            User32.UnhookWinEvent(_hook).CheckValid();
            _hook = User32.HWINEVENTHOOK.NULL;
            _objectDestroyed = null;
        }

        public override void Watch()
        {
            _hook = User32.SetWinEventHook(
                User32.EventConstants.EVENT_OBJECT_DESTROY,
                User32.EventConstants.EVENT_OBJECT_DESTROY,
                HINSTANCE.NULL, _objectDestroyed, 0, 0,
                User32.WINEVENT.WINEVENT_OUTOFCONTEXT).CheckValid();
        }

        private void ObjectDestroyed(User32.HWINEVENTHOOK hwineventhook, uint winevent, HWND hwnd, int idobject,
            int idchild, uint ideventthread, uint dwmseventtime)
        {
            if (idobject != User32.ObjectIdentifiers.OBJID_WINDOW) return;
            Events.OnNext(hwnd);
        }

    }
}
