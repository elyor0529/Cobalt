#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Common.Communication.Messages;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Data.Repository;
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
        private readonly WindowClosedWatcher _winClosedWatcher;
        private readonly ILogger<EngineWorker> _logger;
        private readonly MessageLoop _msgLoop;
        private SystemEventWatcher _sysWatcher;
        private IDbRepository _repo;

        public EngineWorker(ILogger<EngineWorker> logger, EngineService engineSvc, IDbRepository repo)
        {
            _logger = logger;
            _engineSvc = engineSvc;
            _msgLoop = new MessageLoop();
            _winClosedWatcher = new WindowClosedWatcher();
            _fgWinWatcher = new ForegroundWindowWatcher();
            _sysWatcher = new SystemEventWatcher();

            _repo = repo;
        }

        private async Task Work(CancellationToken stoppingToken)
        {
            _fgWinWatcher
                .GroupByUntil(
                    fgSwitch => fgSwitch.Window,
                    kv => _winClosedWatcher.Where(closedHandle => closedHandle == kv.Key.Handle))
                .Subscribe(winSwitches =>
                {
                    _logger.LogWarning("NEW APP {Window}", winSwitches.Key.Title);

                    winSwitches.Subscribe(
                        w =>
                        {
                            _logger.LogInformation("{Time}: {Window} ({HWND})", w.ActivatedTimestamp, w.Window.Title,
                                w.Window.Handle.DangerousGetHandle());
                        }, () => { _logger.LogError("Window Closed!"); });
                });

            _fgWinWatcher.Count().Subscribe(x =>
                _engineSvc.PushUsageSwitch(new UsageSwitch {UsageId = x}));

            _sysWatcher.Subscribe(x =>
            {
                x = _repo.Insert(x);
                _logger.LogInformation("{Timestamp}: {Kind}", x.Timestamp, x.Kind);
            });

            _winClosedWatcher.Watch();
            _fgWinWatcher.Watch();
            _sysWatcher.Watch();

            stoppingToken.Register(() =>
            {
                _winClosedWatcher.Dispose();
                _fgWinWatcher.Dispose();
                _sysWatcher.Dispose();
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