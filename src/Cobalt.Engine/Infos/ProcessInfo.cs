using System;
using Cobalt.Common.Data.Entities;
using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public class ProcessInfo : IEquatable<ProcessInfo>
    {
        public ProcessInfo(uint id, Kernel32.SafeHPROCESS handle, string path, bool isWinStoreApp)
        {
            Id = id;
            Handle = handle;
            Path = path;
            IsWinStoreApp = isWinStoreApp;
        }

        public uint Id { get; }
        public Kernel32.SafeHPROCESS Handle { get; }
        public string Path { get; }
        public bool IsWinStoreApp { get; }
        public App App { get; set; }

        public bool Equals(ProcessInfo other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ProcessInfo) obj);
        }

        public override int GetHashCode()
        {
            return (int) Id;
        }
    }
}