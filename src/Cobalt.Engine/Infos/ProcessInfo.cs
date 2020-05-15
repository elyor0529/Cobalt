using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Utils;
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
            Path = win.Path;
        }

        public uint Id { get; }
        public bool IsWinStoreApp { get; }
        public string Path { get; }

        private AppIdentification _appIdentification = null;
        public async ValueTask<AppIdentification> GetIdentification()
        {
            if (_appIdentification != null) return _appIdentification;
            if (IsWinStoreApp)
            {
                var aumid = GetWindowsStoreAppUserModelId(Handle);

                /*
                 * AppInfo.Find(aumid); // 19041 only
                 */

                /*
                var infos = await AppDiagnosticInfo.RequestInfoForAppAsync(aumid);
                var info = infos[0].AppInfo;
                */

                /*
                Vanara.PInvoke.AdvApi32.OpenProcessToken(Handle, AdvApi32.TokenAccess.TOKEN_ALL_ACCESS, out var token);
                uint len1 = 1024;
                var buffer1 = new StringBuilder((int)len1);
                Kernel32.GetPackageFullNameFromToken(token, ref len1, buffer1).ThrowIfFailed();


                var p = new PackageManager();
                var package = p.FindPackage(buffer1.ToString());
                var apps = Task.Run(() => package.GetAppListEntriesAsync().AsTask()).Result;
                var appList = apps.ToList();
                var d = package.Dependencies.ToList();
                token.Dispose();*/
                _appIdentification = AppIdentification.NewUWP(aumid);
            }
            else
            {
                _appIdentification = AppIdentification.NewWin32(GetPath(Handle));
            }

            return _appIdentification;
        }

        public static string GetWindowsStoreAppUserModelId(Kernel32.SafeHPROCESS handle)
        {
            for (uint sz = 1024;; sz *= 2)
            {
                var buffer = new StringBuilder((int)sz);
                var ret = Kernel32.GetApplicationUserModelId(handle, ref sz, buffer);
                if (ret.Succeeded)
                {
                    return buffer.ToString();
                }
                ret.ThrowUnless(Win32Error.ERROR_INSUFFICIENT_BUFFER);
            }
        }

        public static string GetPath(Kernel32.SafeHPROCESS handle)
        {
            for (uint sz = 1024;; sz *= 2)
            {
                var buffer = new StringBuilder((int)sz);
                var ret = Kernel32.QueryFullProcessImageName(
                    handle,
                    Kernel32.PROCESS_NAME.PROCESS_NAME_WIN32,
                    buffer,
                    ref sz);
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