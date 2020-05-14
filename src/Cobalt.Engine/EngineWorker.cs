#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Common.Communication.Messages;
using Cobalt.Common.Data.Repository;
using Cobalt.Engine.Infos;
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
        private readonly IDbRepository _repo;
        private readonly SystemEventWatcher _sysWatcher;

        public EngineWorker(ILogger<EngineWorker> logger, EngineService engineSvc, IDbRepository repo)
        {
            _logger = logger;
            _engineSvc = engineSvc;
            _msgLoop = new MessageLoop();
            _fgWinWatcher = new ForegroundWindowWatcher();
            _sysWatcher = new SystemEventWatcher();

            _repo = repo;
        }

        private async Task Work(CancellationToken stoppingToken)
        {
            _fgWinWatcher
                .GroupByUntil(
                    fgSwitch => fgSwitch.Window,
                    win => win.Key.Closed)
                .GroupByUntil(
                    win => new ProcessInfo(win.Key.ProcessId),
                    proc => proc.Key.Exited)
                .Subscribe(proc =>
                {
                    _logger.LogWarning("NEW PROCESS {Id}", proc.Key.ProcessId);

                    proc.Subscribe(
                        win =>
                        {
                            _logger.LogWarning("NEW WINDOW {Window}", win.Key.Title);
                            win.Subscribe(switches =>
                            {
                                _logger.LogInformation("{Time}: {Window}", switches.ActivatedTimestamp,
                                    switches.Window.Title);
                            }, () => { _logger.LogError("Window Closed!"); });
                        }, () => { _logger.LogError("Process Closed!"); });
                });

            _fgWinWatcher.Count().Subscribe(x =>
                _engineSvc.PushUsageSwitch(new UsageSwitch {UsageId = x}));

            _sysWatcher.Subscribe(x =>
            {
                x = _repo.Insert(x);
                _logger.LogInformation("{Timestamp}: {Kind}", x.Timestamp, x.Kind);
            });

            _fgWinWatcher.Watch();
            //_sysWatcher.Watch();

            var sync = SynchronizationContext.Current;

            stoppingToken.Register(() =>
            {
                SynchronizationContext.SetSynchronizationContext(sync);
                _fgWinWatcher.Dispose();
                _sysWatcher.Dispose();
                _msgLoop.Quit();
            }, true);
            _msgLoop.Run();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () => await Work(stoppingToken), stoppingToken);
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously