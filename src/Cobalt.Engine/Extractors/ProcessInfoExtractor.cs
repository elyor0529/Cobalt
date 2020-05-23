using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Utils;
using Cobalt.Engine.Infos;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;
using WinRT;

namespace Cobalt.Engine.Extractors
{
    public interface IProcessInfoExtractor
    {
        ProcessInfo Extract(WindowInfo win);
        IObservable<Unit> Exited(ProcessInfo proc);
        ValueTask<AppIdentification> GetIdentification(ProcessInfo proc);
        void Dispose(ProcessInfo proc);
        string GetWindowsStoreAppUserModelId(Kernel32.SafeHPROCESS handle);
        string GetCommandLine(Kernel32.SafeHPROCESS handle);
        string GetPath(Kernel32.SafeHPROCESS handle);
    }

    public class ProcessInfoExtractor : IProcessInfoExtractor
    {
        private readonly ILogger<ProcessInfoExtractor> _logger;

        public ProcessInfoExtractor(ILogger<ProcessInfoExtractor> logger)
        {
            _logger = logger;
        }

        public ProcessInfo Extract(WindowInfo win)
        {
            var pid = win.ProcessId;
            var path = win.Path;
            var isWinStoreApp = win.IsWinStoreApp;
            var handle =
                Kernel32.OpenProcess(
                        ACCESS_MASK.FromEnum(
                            Kernel32.ProcessAccess.PROCESS_VM_READ |
                            Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION) |
                        ACCESS_MASK.SYNCHRONIZE,
                        false,
                        pid)
                    .CheckValid();
            return new ProcessInfo(pid, handle, path, isWinStoreApp);
        }

        public IObservable<Unit> Exited(ProcessInfo proc)
        {
            return Observable.Create<Unit>(obs =>
            {
                Kernel32.SafeRegisteredWaitHandle wait = null;
                Kernel32.WaitOrTimerCallback callback = (ctx, fired) =>
                {
                    obs.OnNext(Unit.Default);
                    obs.OnCompleted();
                    wait?.Dispose();
                };
                Kernel32.RegisterWaitForSingleObject(out wait, proc.Handle, callback, IntPtr.Zero,
                    Kernel32.INFINITE,
                    Kernel32.WT.WT_EXECUTEONLYONCE).CheckValid();
                wait.CompletionEvent = Kernel32.SafeEventHandle.InvalidHandle;

                return () => { callback = null; };
            });
        }

        public ValueTask<AppIdentification> GetIdentification(ProcessInfo proc)
        {
            if (proc.IsWinStoreApp)
            {
                var aumid = GetWindowsStoreAppUserModelId(proc.Handle);
                _logger.LogDebug("Extracted {aumid} for UWP", aumid);
                return new ValueTask<AppIdentification>(AppIdentification.NewUWP(aumid));
            }

            return new ValueTask<AppIdentification>(AppIdentification.NewWin32(proc.Path));
        }

        public string GetCommandLine(Kernel32.SafeHPROCESS handle)
        {
            var info = NtDll.NtQueryInformationProcess<NtDll.PROCESS_BASIC_INFORMATION>(handle,
                NtDll.PROCESSINFOCLASS.ProcessBasicInformation);

            var pebSz = Marshal.SizeOf<NtDll.PEB>();
            var pebPtr = Marshal.AllocHGlobal(pebSz);
            Kernel32.ReadProcessMemory(handle, info.AsRef().PebBaseAddress, pebPtr, pebSz, out var pebSzRead)
                .CheckValid();
            var peb = Marshal.PtrToStructure<NtDll.PEB>(pebPtr);
            Marshal.FreeHGlobal(pebPtr);

            var rtlUserParamsSz = Marshal.SizeOf<NtDll.RTL_USER_PROCESS_PARAMETERS>();
            var rtlUserParamsPtr = Marshal.AllocHGlobal(rtlUserParamsSz);
            Kernel32.ReadProcessMemory(handle, peb.ProcessParameters, rtlUserParamsPtr, rtlUserParamsSz,
                out var rtlUserParamsRead).CheckValid();
            /* errors out below with ExecutionEngineException*/
            var rtlUserParams = Marshal.PtrToStructure<NtDll.RTL_USER_PROCESS_PARAMETERS>(rtlUserParamsPtr);
            Marshal.FreeHGlobal(rtlUserParamsPtr);

            return rtlUserParams.CommandLine.Buffer; // lets test if this works
        }

        public void Dispose(ProcessInfo proc)
        {
            proc.Handle.Dispose();
        }

        public string GetWindowsStoreAppUserModelId(Kernel32.SafeHPROCESS handle)
        {
            for (uint sz = 1024;; sz *= 2)
            {
                var buffer = new StringBuilder((int) sz);
                var ret = Kernel32.GetApplicationUserModelId(handle, ref sz, buffer);
                if (ret.Succeeded) return buffer.ToString();
                ret.ThrowUnless(Win32Error.ERROR_INSUFFICIENT_BUFFER);
            }
        }

        public string GetPath(Kernel32.SafeHPROCESS handle)
        {
            for (uint sz = 1024;; sz *= 2)
            {
                var buffer = new StringBuilder((int) sz);
                var ret = Kernel32.QueryFullProcessImageName(
                    handle,
                    Kernel32.PROCESS_NAME.PROCESS_NAME_WIN32,
                    buffer,
                    ref sz);
                if (ret) return buffer.ToString();
                Win32Error.ThrowLastErrorUnless(Win32Error.ERROR_INSUFFICIENT_BUFFER);
            }
        }
    }
}