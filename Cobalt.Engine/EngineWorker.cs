using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Cobalt.Engine
{
    public class EngineWorker : IHostedService
    {
        private readonly MessageLoop _msgLoop;
        private readonly ForegroundWindowWatcher _fgWinWatcher;

        public EngineWorker()
        {
            _msgLoop = new MessageLoop();
            _fgWinWatcher = new ForegroundWindowWatcher();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _fgWinWatcher.WindowChanged.Subscribe(x => Console.WriteLine($"{x.Item2}: {x.Item1}"));

            _fgWinWatcher.Watch();
            _msgLoop.Run();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _fgWinWatcher.Dispose();
            _msgLoop.Quit();
        }
    }
}
