using System;
using System.Reactive;
using System.Runtime.InteropServices;
using Cobalt.Engine.Native;

namespace Cobalt.Engine.Infos
{
    public class Process : IDisposable
    {
        private static IObservable<Unit> ExitedWatcher(IntPtr handle) =>
            new Watcher<Unit>(x => Methods.process_exit_begin(x, handle), x => { });

        public Process(uint id) : this(new Basic { Id = id })
        {
        }

        internal Process(Window window) : this(process_id_for_window(window.Handle))
        {
        }

        private Process(Basic basic)
        {
            _basic = basic;
            _extended = new Lazy<Extended>(() => process_information(Id));
        }

        private Basic _basic;
        private readonly Lazy<Extended> _extended = null;

        public uint Id => _basic.Id;
        public IntPtr Handle => _extended.Value.Handle;
        public string Path => _extended.Value.Path.ToString();
        public string CmdLine => _extended.Value.CmdLine.ToString();
        public string Name => _extended.Value.Name.ToString();
        public string Description => _extended.Value.Description.ToString();

        public IObservable<Unit> Exited => ExitedWatcher(Handle);

        [DllImport(Constants.NativeLibrary)] 
        public static extern Extended process_information(uint id);

        [DllImport(Constants.NativeLibrary)]
        public static extern Basic process_id_for_window(IntPtr win);

        public void Dispose()
        {
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
    }
}
