using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Cobalt.Common.Transmission;
using Cobalt.Common.Transmission.Messages;
using Grpc.Core;
using ProtoBuf.Grpc;
using Vanara.PInvoke;

namespace Cobalt.Engine
{
    public class Program
    {
        private static TransmissionServer _server;
        private static EngineService _engineSvc;

        private static void Main(string[] args)
        {
            _engineSvc = new EngineService();
            _server = new TransmissionServer(_engineSvc);
            _server.StartServer();

            StartThread(0);
            StartThread(1);

            // TODO wait for new version of Vanara, then use User32.EventConstants.* instead of 3
            User32.SetWinEventHook(
                3,
                3,
                IntPtr.Zero,
                Callback,
                0,
                0,
                User32.WINEVENT.WINEVENT_OUTOFCONTEXT);

            var msgLoop = new MessageLoop();
            msgLoop.Run();
        }

        public static void StartThread(int num)
        {
            new Thread(async x =>
            {
                var c = new TransmissionClient();
                var client = c.EngineService();
                var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                var options = new CallOptions(cancellationToken: cancel.Token);
                await foreach (var s in client.AppSwitches(/*,new CallContext(options)*/))
                {
                    Console.WriteLine($"[${num} THREAD] {s.AppName}: {s.AppCommandLine} | {s.AppDescription}");
                }

            }).Start();
        }

        private static void Callback(User32.HWINEVENTHOOK hWinEventHook, uint winEvent, HWND hwnd, int idObject,
            int idChild, uint idEventThread, uint dwmsEventTime)
        {
            try
            {

                var dwmsTimestamp = DateTimeOffset.Now.AddMilliseconds(dwmsEventTime - Environment.TickCount);
                var tid = User32.GetWindowThreadProcessId(hwnd, out var pid);
                var proc = Kernel32.OpenProcess(
                    ACCESS_MASK.FromEnum(Kernel32.ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION |
                                         Kernel32.ProcessAccess.PROCESS_VM_READ), false, pid);

                var pathLen = 512;
                var path = new StringBuilder(pathLen);
                var success = QueryFullProcessImageName(proc.DangerousGetHandle(), 0, path, ref pathLen);

                if (!success)
                {
                    Console.WriteLine($"[ERR] Unable to find path for {pid}:{tid}");
                    return;
                }

                var info = FileVersionInfo.GetVersionInfo(path.ToString());


                var pbi = new PROCESS_BASIC_INFORMATION();
                NtQueryInformationProcess(proc.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation,
                    ref pbi, pbi.Size, out _);

                var peb = ReadProcessMemory<PEB>(proc, pbi.PebBaseAddress);
                var pparams = ReadProcessMemory<RTL_USER_PROCESS_PARAMETERS>(proc, peb.ProcessParameters);
                var cmd = pparams.CommandLine.ToString(proc);

                _engineSvc.PushAppSwitch(new AppSwitchMessage
                    {AppCommandLine = cmd, AppDescription = info.FileDescription, AppName = path.ToString()});
            }
            catch (Exception e)
            {

            }
        }

        #region Win32

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags,
            [Out] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("NTDLL.DLL", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr hProcess, PROCESSINFOCLASS pic,
            ref PROCESS_BASIC_INFORMATION pbi, int cb, out int pSize);

        public enum PROCESSINFOCLASS
        {
            ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
            ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
            ProcessIoCounters, // q: IO_COUNTERS
            ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
            ProcessTimes, // q: KERNEL_USER_TIMES
            ProcessBasePriority, // s: KPRIORITY
            ProcessRaisePriority, // s: ULONG
            ProcessDebugPort, // q: HANDLE
            ProcessExceptionPort, // s: HANDLE
            ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
            ProcessLdtInformation, // 10
            ProcessLdtSize,
            ProcessDefaultHardErrorMode, // qs: ULONG
            ProcessIoPortHandlers, // (kernel-mode only)
            ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
            ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[], s: void
            ProcessUserModeIOPL,
            ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
            ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
            ProcessWx86Information,
            ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
            ProcessAffinityMask, // s: KAFFINITY
            ProcessPriorityBoost, // qs: ULONG
            ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
            ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
            ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
            ProcessWow64Information, // q: ULONG_PTR
            ProcessImageFileName, // q: UNICODE_STRING
            ProcessLUIDDeviceMapsEnabled, // q: ULONG
            ProcessBreakOnTermination, // qs: ULONG
            ProcessDebugObjectHandle, // 30, q: HANDLE
            ProcessDebugFlags, // qs: ULONG
            ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY, s: size 0 disables, otherwise enables
            ProcessIoPriority, // qs: ULONG
            ProcessExecuteFlags, // qs: ULONG
            ProcessResourceManagement,
            ProcessCookie, // q: ULONG
            ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
            ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
            ProcessPagePriority, // q: ULONG
            ProcessInstrumentationCallback, // 40
            ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
            ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
            ProcessImageFileNameWin32, // q: UNICODE_STRING
            ProcessImageFileMapping, // q: HANDLE (input)
            ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
            ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
            ProcessGroupInformation, // q: USHORT[]
            ProcessTokenVirtualizationEnabled, // s: ULONG
            ProcessConsoleHostProcess, // q: ULONG_PTR
            ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
            ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
            ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
            ProcessDynamicFunctionTableInformation,
            ProcessHandleCheckingMode,
            ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
            ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
            MaxProcessInfoClass
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;

            public string ToString(HPROCESS proc)
            {
                var s = new string('\0', Length / 2);
                var ptr = Marshal.StringToHGlobalUni(s);
                if (!Kernel32.ReadProcessMemory(proc, Buffer, ptr, new SizeT(Length), out _))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                s = Marshal.PtrToStringUni(ptr);
                Marshal.FreeHGlobal(ptr);
                return s;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public UIntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;

            public int Size => Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
        }

        //use this for more expansive structure: https://docs.rs/ntapi/0.2.0/ntapi/ntpebteb/struct.PEB.html
        [StructLayout(LayoutKind.Explicit, Size = 0x40)]
        public struct PEB
        {
            [FieldOffset(0x000)] public byte InheritedAddressSpace;
            [FieldOffset(0x001)] public byte ReadImageFileExecOptions;
            [FieldOffset(0x002)] public byte BeingDebugged;
            [FieldOffset(0x003)] public byte Spare;
            [FieldOffset(0x008)] public IntPtr Mutant;
            [FieldOffset(0x010)] public IntPtr ImageBaseAddress; // (PVOID) 
            [FieldOffset(0x018)] public IntPtr Ldr; // (PPEB_LDR_DATA)
            [FieldOffset(0x020)] public IntPtr ProcessParameters; // (PRTL_USER_PROCESS_PARAMETERS)
            [FieldOffset(0x028)] public IntPtr SubSystemData; // (PVOID) 
            [FieldOffset(0x030)] public IntPtr ProcessHeap; // (PVOID) 
            [FieldOffset(0x038)] public IntPtr FastPebLock; // (PRTL_CRITICAL_SECTION)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RTL_USER_PROCESS_PARAMETERS
        {
            public uint MaximumLength;
            public uint Length;
            public uint Flags;
            public uint DebugFlags;
            public IntPtr ConsoleHandle;
            public uint ConsoleFlags;
            public IntPtr StdInputHandle;
            public IntPtr StdOutputHandle;
            public IntPtr StdErrorHandle;
            public UNICODE_STRING CurrentDirectoryPath;
            public IntPtr CurrentDirectoryHandle;
            public UNICODE_STRING DllPath;
            public UNICODE_STRING ImagePathName;
            public UNICODE_STRING CommandLine;
        }

        public static T ReadProcessMemory<T>(HPROCESS hProcess, IntPtr lpBaseAddress)
            where T : struct
        {
            var rsz = Marshal.SizeOf<T>();
            var pnt = Marshal.AllocHGlobal(rsz);
            if (!Kernel32.ReadProcessMemory(hProcess, lpBaseAddress, pnt, rsz, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            var ret = Marshal.PtrToStructure<T>(pnt);
            Marshal.FreeHGlobal(pnt);
            return ret;
        }

        #endregion
    }
}