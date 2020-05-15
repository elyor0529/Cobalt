﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Utils;
using Microsoft.AspNetCore.Routing.Constraints;
using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public class ProcessInfo : IEquatable<ProcessInfo>, IDisposable
    {
        private Kernel32.SafeHPROCESS _handle;

        public ProcessInfo(BasicWindowInfo win)
        {
            Id = win.ProcessId;
            IsWinStoreApp = win.IsWinStoreApp;
        }

        public uint Id { get; }
        public bool IsWinStoreApp { get; }

        private AppIdentification _identification;
        public AppIdentification Identification => _identification ??= GetIdentification();

        private AppIdentification GetIdentification()
        {
            if (IsWinStoreApp)
            {
                uint len = 1024;
                var buffer = new StringBuilder((int)len);
                Kernel32.GetApplicationUserModelId(Handle, ref len, buffer).ThrowIfFailed();
                Vanara.PInvoke.AdvApi32.OpenProcessToken(Handle, AdvApi32.TokenAccess.TOKEN_ALL_ACCESS, out var token);
                uint len1 = 1024;
                var buffer1 = new StringBuilder((int)len1);
                Kernel32.GetPackageFullNameFromToken(token, ref len1, buffer1).ThrowIfFailed();
                Kernel32.PACKAGE_INFO_REFERENCE info = new Kernel32.PACKAGE_INFO_REFERENCE();
                Kernel32.OpenPackageInfoByFullName(buffer1.ToString(), 0, ref info).ThrowIfFailed();

                uint infoLen = 128;
                uint infoLen2 = (uint)Marshal.SizeOf<Kernel32.PACKAGE_INFO>() * infoLen;
                var infos = new Kernel32.PACKAGE_INFO[infoLen];
                var hdl = GCHandle.Alloc(infos, GCHandleType.Pinned);
                Kernel32.GetPackageInfo2(info, 0, Kernel32.PackagePathType.PackagePathType_Effective, ref infoLen2, hdl.AddrOfPinnedObject(),
                    out infoLen).ThrowIfFailed();



                hdl.Free();
                return AppIdentification.NewUWP(buffer.ToString());
            }
            else
            {
                return AppIdentification.NewWin32(GetPath(Handle));
            }
        }

        public static string GetPath(Kernel32.SafeHPROCESS handle)
        {
            for (uint pathSz = 1024;; pathSz *= 2)
            {
                var buffer = new StringBuilder((int)pathSz);
                var ret = Kernel32.QueryFullProcessImageName(
                    handle,
                    Kernel32.PROCESS_NAME.PROCESS_NAME_WIN32,
                    buffer,
                    ref pathSz);
                if (ret)
                {
                    return buffer.ToString();
                }
                Win32Error.ThrowLastErrorUnless(Win32Error.ERROR_INSUFFICIENT_BUFFER);
            }
        }

        public Kernel32.SafeHPROCESS Handle
        {
            get
            {
                if (_handle == null)
                    _handle =
                        Kernel32.OpenProcess(
                                ACCESS_MASK.FromEnum(
                                    Kernel32.ProcessAccess.PROCESS_VM_READ |
                                    Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION) |
                                ACCESS_MASK.SYNCHRONIZE,
                                false,
                                Id)
                            .CheckValid();
                return _handle;
            }
        }

        public IObservable<Unit> Exited
            => Observable.Create<Unit>(obs =>
            {
                Kernel32.SafeRegisteredWaitHandle wait = null;
                Kernel32.WaitOrTimerCallback callback = (ctx, fired) =>
                {
                    obs.OnNext(Unit.Default);
                    obs.OnCompleted();
                    wait?.Dispose();
                };
                Kernel32.RegisterWaitForSingleObject(out wait, Handle, callback, IntPtr.Zero,
                    Kernel32.INFINITE,
                    Kernel32.WT.WT_EXECUTEONLYONCE).CheckValid();
                wait.CompletionEvent = Kernel32.SafeEventHandle.InvalidHandle;

                return () => { callback = null; };
            });

        public void Dispose()
        {
            Handle.Dispose();
        }


        public bool Equals(ProcessInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProcessInfo) obj);
        }

        public override int GetHashCode()
        {
            return (int) Id;
        }
    }
}