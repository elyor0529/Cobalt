using System;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Engine.Watchers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace Cobalt.Engine
{
    public class EngineWorker : IHostedService
    {
        private readonly ForegroundWindowWatcher _fgWinWatcher;
        private readonly ILogger<EngineWorker> _logger;
        private readonly MessageLoop _msgLoop;

        public EngineWorker(ILogger<EngineWorker> logger)
        {
            _logger = logger;
            _msgLoop = new MessageLoop();
            _fgWinWatcher = new ForegroundWindowWatcher();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _fgWinWatcher.Subscribe(x =>
                _logger.LogWarning("{Timestamp}: {Path}", x.ActivatedTimestamp, x.ProcessFilePath));

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