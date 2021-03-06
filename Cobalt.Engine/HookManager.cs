﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cobalt.Engine
{
    public class HookManager
    {
        /// <summary>
        ///     Call this after setting up hooks
        /// </summary>
        public void EventLoop()
        {
            //keep getting messages
            while (true)
            {
                if (Win32.GetMessage(out var msg, IntPtr.Zero, 0, 0) == 0) break;

                //even ms docs say that you dont need to understand these
                Win32.TranslateMessage(ref msg);
                Win32.DispatchMessage(ref msg);
            }
        }

        public void WinEventHook(Win32.WinEvent @event, Win32.WinEventProc callback)
        {
            WinEventHookRange(@event, @event, callback);
        }

        public void WinEventHookRange(Win32.WinEvent min, Win32.WinEvent max, Win32.WinEventProc callback)
        {
            var windowEventHook = Win32.SetWinEventHook(
                (int) min, // eventMin
                (int) max, // eventMax
                IntPtr.Zero, // hmodWinEventProc
                callback, // lpfnWinEventProc
                0, // idProcess 
                0, // idThread 
                //since both of the above params are 0, its  a global hook
                Win32.WINEVENT_OUTOFCONTEXT);

            if (windowEventHook == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void WindowsHook(Win32.HookType type, Win32.HookProc hook)
        {
            var hMod = Win32.GetModuleHandle(
                Process.GetCurrentProcess().MainModule.ModuleName);
            Win32.SetWindowsHookEx(type, hook, hMod, 0);
        }
    }
}