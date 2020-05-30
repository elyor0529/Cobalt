using System;
using Cobalt.Common.Data.Entities;
using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public class WindowInfo : IEquatable<WindowInfo>
    {
        public WindowInfo(HWND handle, string title, string path, uint tid, uint pid, bool isWinStoreApp)
        {
            Handle = handle;
            Title = title;
            Path = path;
            ThreadId = tid;
            ProcessId = pid;
            IsWinStoreApp = isWinStoreApp;
        }

        public uint ProcessId { get; }
        public uint ThreadId { get; }
        public HWND Handle { get; }
        public string Title { get; }
        public string Path { get; }
        public bool IsWinStoreApp { get; }
        public Session Session { get; set; }

        public bool Equals(WindowInfo other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ProcessId == other.ProcessId && ThreadId == other.ThreadId && Handle.Equals(other.Handle) &&
                   Title == other.Title;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((WindowInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProcessId, ThreadId, Handle, Title);
        }
    }
}