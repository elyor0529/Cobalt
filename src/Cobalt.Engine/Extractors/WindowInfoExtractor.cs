﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using System.Text;
using Cobalt.Common.Utils;
using Cobalt.Engine.Infos;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace Cobalt.Engine.Extractors
{
    public interface IWindowInfoExtractor
    {
        WindowInfo Extract(BasicWindowInfo basicWin);
        IObservable<Unit> Closed(WindowInfo win);
        void Dispose(WindowInfo win);
    }

    public class WindowInfoExtractor : IWindowInfoExtractor
    {
        public WindowInfoExtractor(ILogger<WindowInfoExtractor> logger, IProcessInfoExtractor processInfo)
        {
            _logger = logger;
            _processInfo = processInfo;
        }

        private static readonly string ApplicationFrameHost = @"C:\Windows\System32\ApplicationFrameHost.exe";
        private readonly ILogger<WindowInfoExtractor> _logger;
        private readonly IProcessInfoExtractor _processInfo;

        public WindowInfo Extract(BasicWindowInfo basicWin)
        {
            var handle = basicWin.Handle;
            var title = basicWin.Title;
            var isWinStoreApp = false;
            var tid = User32.GetWindowThreadProcessId(handle, out var pid);

            using var proc = Kernel32.OpenProcess(
                ACCESS_MASK.FromEnum(Kernel32.ProcessAccess.PROCESS_VM_READ |
                                     Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION), false, pid);

            var path = _processInfo.GetPath(proc);

            if (ApplicationFrameHost.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Found UWP Window, extracting...");
                var appWin = HWND.NULL;
                while (appWin == HWND.NULL)
                    User32.EnumChildWindows(handle, (hdl, id) =>
                    {
                        User32.GetWindowThreadProcessId(hdl, out var cpid);
                        if (cpid == pid) return true;

                        appWin = hdl;
                        return false;
                    }, IntPtr.Zero);
                handle = appWin;
                tid = User32.GetWindowThreadProcessId(handle, out pid);
                path = _processInfo.GetPath(proc);
                isWinStoreApp = true;
            }
            // TODO check if java process here

            return new WindowInfo(handle, title, path, tid, pid, isWinStoreApp);
        }

        public IObservable<Unit> Closed(WindowInfo win) =>
            Observable.Create<Unit>(obs =>
            {
                User32.WinEventProc windowClosed = (eventHook, evnt, hwnd, idObject, child, thread, time) =>
                {
                    if (idObject != User32.ObjectIdentifiers.OBJID_WINDOW || child != 0 || hwnd != win.Handle) return;
                    obs.OnNext(Unit.Default);
                    obs.OnCompleted();
                    User32.UnhookWinEvent(eventHook).CheckValid();
                };
                User32.SetWinEventHook(
                    User32.EventConstants.EVENT_OBJECT_DESTROY,
                    User32.EventConstants.EVENT_OBJECT_DESTROY,
                    HINSTANCE.NULL, windowClosed, win.ProcessId, win.ThreadId,
                    User32.WINEVENT.WINEVENT_OUTOFCONTEXT).CheckValid();
                return () => { windowClosed = null; };
            });

        public void Dispose(WindowInfo win)
        {
            // do nothing here
        }
    }
}