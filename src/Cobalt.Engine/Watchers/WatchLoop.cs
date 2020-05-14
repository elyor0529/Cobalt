using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Cobalt.Engine.Watchers
{
    public class WatchLoop
    {
        public ValueTask Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            while (User32.PeekMessage(out var msg, wRemoveMsg: User32.PM.PM_REMOVE))
            {
                if (msg.message == (uint) User32.WindowMessage.WM_QUIT) break;
                User32.TranslateMessage(msg);
                User32.DispatchMessage(msg);
            }

            return new ValueTask();
        }
    }
}