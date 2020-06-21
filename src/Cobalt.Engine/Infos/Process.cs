using System;
using System.Reactive;
using System.Runtime.InteropServices;
using Cobalt.Common.Data.Entities;
using Cobalt.Engine.Native;
using Serilog;

namespace Cobalt.Engine.Infos
{
    // TODO reorder all these so that they make more sense
    // TODO shift native shit to another dll?
    public class Process : IDisposable, IEquatable<Process>
    {
        private static IObservable<Unit> ExitedWatcher(IntPtr handle) =>
            new Watcher<Unit>(x => Methods.process_exit_begin(x, handle), x => { });

        public Process(Window window)
        {
            _basic = process_id_for_window(window.Handle);
            _extended = new Lazy<Extended>(() => process_information(Id));
            UwpAumid = window.UwpAumid;
        }

        private readonly Basic _basic;
        private readonly Lazy<Extended> _extended = null;

        public uint Id => _basic.Id;
        public IntPtr Handle => _extended.Value.Handle;
        public string Path => _extended.Value.Path.ToString();
        public string CmdLine => _extended.Value.CmdLine.ToString();
        public string Name => _extended.Value.Name.ToString();
        public string Description => _extended.Value.Description.ToString();
        public string UwpAumid { get; }

        public IObservable<Unit> Exited => ExitedWatcher(Handle);

        [DllImport(Constants.NativeLibrary)] 
        public static extern Extended process_information(uint id);

        [DllImport(Constants.NativeLibrary)]
        public static extern Basic process_id_for_window(IntPtr win);

        public void Dispose()
        {
            Log.Information("Process Exited!");
            // TODO dispose here
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Extended
        {
            public uint Id;
            public IntPtr Handle;
            public FfiString Path;
            public FfiString CmdLine;
            public FfiString Name;
            public FfiString Description;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Basic
        {
            public uint Id;
        }

        public AppIdentification GetIdentification(Process proc)
        {
            // TODO check java?
            return UwpAumid != null ? AppIdentification.NewUWP(UwpAumid) : AppIdentification.NewWin32(Path);
        }

        public bool Equals(Process other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _basic.Equals(other._basic);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Process) obj);
        }

        public override int GetHashCode()
        {
            return _basic.GetHashCode();
        }

        public static bool operator ==(Process left, Process right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Process left, Process right)
        {
            return !Equals(left, right);
        }
    }
}
