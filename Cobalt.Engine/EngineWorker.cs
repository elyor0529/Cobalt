using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Vanara.PInvoke;

namespace Cobalt.Engine
{
    public class EngineWorker : IHostedService
    {
        private readonly ForegroundWindowWatcher _fgWinWatcher;
        private readonly MessageLoop _msgLoop;

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