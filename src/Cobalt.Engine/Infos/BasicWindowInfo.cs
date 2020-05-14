using System;
using System.Collections.Generic;
using System.Text;
using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public struct BasicWindowInfo
    {
        public BasicWindowInfo(uint pid, uint tid, HWND hWnd, string title)
        {
            ProcessId = pid;
            ThreadId = tid;
            Handle = hWnd;
            Title = title;
        }

        public uint ProcessId { get; }
        public uint ThreadId { get; }
        public HWND Handle { get; }
        public string Title { get; }

    }
}
