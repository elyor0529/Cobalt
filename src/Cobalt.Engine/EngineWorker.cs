using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Common.Communication.Messages;
using Cobalt.Engine.Watchers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace Cobalt.Engine
{
    public class EngineWorker : BackgroundService, IHostedService
    {
        private readonly EngineService _engineSvc;
        private readonly ForegroundWindowWatcher _fgWinWatcher;
        private readonly ILogger<EngineWorker> _logger;
        private readonly MessageLoop _msgLoop;

        public EngineWorker(ILogger<EngineWorker> logger, EngineService engineSvc)
        {
            _logger = logger;
            _engineSvc = engineSvc;
            _msgLoop = new MessageLoop();
            _fgWinWatcher = new ForegroundWindowWatcher();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() =>
            {
                _fgWinWatcher.Subscribe(x =>
                    _logger.LogDebug("{Timestamp}: {Path}", x.ActivatedTimestamp, x.ProcessFilePath));

                _fgWinWatcher.Count().Subscribe(x =>
                    _engineSvc.PushForegroundWindowSwitch(new ForegroundWindowSwitch {WindowId = x}));

                _fgWinWatcher.Watch();

                stoppingToken.Register(() =>
                {
                    _fgWinWatcher.Dispose();
                    _msgLoop.Quit();
                });
                _msgLoop.Run();
            }, stoppingToken);
        }
    }
}