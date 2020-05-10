#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Common.Communication.Messages;
using Cobalt.Engine.Services;
using Cobalt.Engine.Watchers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace Cobalt.Engine
{
    public class EngineWorker : BackgroundService
    {
        private readonly EngineService _engineSvc;
        private readonly ForegroundWindowWatcher _fgWinWatcher;
        private readonly ILogger<EngineWorker> _logger;
        private readonly MessageLoop _msgLoop;
        private SystemEventWatcher _sysWatcher;

        public EngineWorker(ILogger<EngineWorker> logger, EngineService engineSvc)
        {
            _logger = logger;
            _engineSvc = engineSvc;
            _msgLoop = new MessageLoop();
            _fgWinWatcher = new ForegroundWindowWatcher();
            _sysWatcher = new SystemEventWatcher();
        }

        private async Task Work(CancellationToken stoppingToken)
        {
            _fgWinWatcher.Subscribe(x =>
                _logger.LogDebug("{Timestamp}: {Path}", x.ActivatedTimestamp, x.ProcessFilePath));

            _fgWinWatcher.Count().Subscribe(x =>
                _engineSvc.PushUsageSwitch(new UsageSwitch {UsageId = x}));

            _sysWatcher.Subscribe(x => _logger.LogDebug("{Timestamp}: {Kind}", x.Timestamp.ToString(), x.Kind.ToString()));

            _fgWinWatcher.Watch();
            _sysWatcher.Watch();

            stoppingToken.Register(() =>
            {
                _fgWinWatcher.Dispose();
                _msgLoop.Quit();
            });
            _msgLoop.Run();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () => await Work(stoppingToken), stoppingToken);
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously