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

namespace Cobalt.Engine
{
    public class EngineWorker : BackgroundService
    {
        private readonly EngineService _engineSvc;
        private readonly ForegroundWindowWatcher _fgWinWatcher;
        private readonly ILogger<EngineWorker> _logger;
        private readonly IDbRepository _repo;
        private readonly SystemEventWatcher _sysWatcher;
        private readonly WatchLoop _watchLoop;

        public EngineWorker(ILogger<EngineWorker> logger, EngineService engineSvc, IDbRepository repo)
        {
            _logger = logger;
            _engineSvc = engineSvc;
            _watchLoop = new WatchLoop();
            _fgWinWatcher = new ForegroundWindowWatcher();
            _sysWatcher = new SystemEventWatcher();

            _repo = repo;
        }

        private async Task Work(CancellationToken stoppingToken)
        {
            // make window and processinfo idisposable, then call dispose when they get closed and exited

            _fgWinWatcher
                .GroupByUntil(
                    sw => sw.Window,
                    win => win.Key.Closed)
                .GroupByUntil(
                    win => new ProcessInfo(win.Key),
                    proc => proc.Key.Exited)
                .SelectMany(proc =>
                {
                    return proc
                            .Finally(async () =>
                            {
                                proc.Key.Dispose();
                                _logger.LogError("APP {} closed", await proc.Key.GetIdentification());
                            })
                            .SelectMany(win => win
                                .Do(w => w.Window.Process = proc.Key).Finally(() =>
                                {
                                    win.Key.Dispose();
                                    _logger.LogError("window {} closed", win.Key.Title);
                                }))
                        ;
                })
                .Buffer(2, 1)
                .Select(sws => new ForegroundWindowUsage(sws[0], sws[1]))
                .Subscribe(async proc =>
                {
                    _logger.LogInformation("{s} - {e} Switch `{win1}` ({app1}) to `{win2}` ({app2})",
                        proc.Start, proc.End,
                        proc.CurrentWindow.Title, await proc.CurrentWindow.Process.GetIdentification(),
                        proc.NewWindow.Title, await proc.NewWindow.Process.GetIdentification());
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

            await _watchLoop.Run(stoppingToken);

            _fgWinWatcher.Dispose();
            //_sysWatcher.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () => await Work(stoppingToken), stoppingToken);
        }
    }
}