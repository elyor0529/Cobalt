using System;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Cobalt.Engine.Watchers
{
    public class WatchLoop
    {
        public ValueTask<int> Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            while (User32.PeekMessage(out var msg, wRemoveMsg: User32.PM.PM_REMOVE))
            {
                if (msg.message == (uint) User32.WindowMessage.WM_QUIT) return new ValueTask<int>(msg.wParam.ToInt32());
                User32.TranslateMessage(msg);
                User32.DispatchMessage(msg);
            }

            return new ValueTask<int>(0);
        }
    }
}