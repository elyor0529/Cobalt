using System;
using System.Diagnostics;
using System.Threading;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Utils;
using Vanara.PInvoke;

namespace Cobalt.Engine.Watchers
{
    public class SystemEventWatcher : Watcher<SystemEvent>
    {
        private readonly TimeSpan _idleDuration = TimeSpan.FromSeconds(5); // TODO allow this to be configurable
        private Timer _idleTimer;
        private bool _isActive = true;
        private User32.HookProc _keyCallback;
        private User32.SafeHHOOK _keyHook;
        private DateTime _lastActive = DateTime.Now;
        private User32.HookProc _mouseCallback;
        private User32.SafeHHOOK _mouseHook;

        private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0) ActiveCallback();
            return User32.CallNextHookEx(_keyHook, nCode, wParam, lParam);
        }

        private IntPtr KeyCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0) ActiveCallback();
            return User32.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void ActiveCallback()
        {
            _idleTimer.Change(_idleDuration, _idleDuration);
            _lastActive = DateTime.Now;
            // if it was active before, do nothing
            if (_isActive) return;
            _isActive = true;
            Events.OnNext(new SystemEvent {Kind = SystemEventKind.Active, Timestamp = _lastActive});
        }

        private void IdleCallback(object state)
        {
            // if it was idle before, do nothing
            if (!_isActive) return;
            Events.OnNext(new SystemEvent {Kind = SystemEventKind.Idle, Timestamp = _lastActive});
            _isActive = false;
        }

        public override void Dispose()
        {
            User32.UnhookWindowsHookEx(_keyHook).CheckValid();
            User32.UnhookWindowsHookEx(_mouseHook).CheckValid();
            _idleTimer.Dispose();
            _keyCallback = null;
            _mouseCallback = null;
            _keyHook = null;
            _mouseHook = null;
            _idleTimer = null;
        }

        public override void Watch()
        {
            _mouseCallback = MouseCallback;
            _keyCallback = KeyCallback;
            var currentMod = Kernel32.GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule?.ModuleName);
            _keyHook = User32.SetWindowsHookEx(User32.HookType.WH_KEYBOARD_LL, _keyCallback, currentMod, 0);
            _mouseHook = User32.SetWindowsHookEx(User32.HookType.WH_MOUSE_LL, _mouseCallback, currentMod, 0);
            _idleTimer = new Timer(IdleCallback, null, _idleDuration, _idleDuration);

            // TODO logon/logoff watcher
        }
    }
}