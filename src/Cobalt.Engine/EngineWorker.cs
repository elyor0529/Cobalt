﻿using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Common.Communication.Messages;
using Cobalt.Common.Data.Entities;
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

        private async ValueTask WithSession(BasicWindowInfo win, ProcessInfo proc)
        {
            // TODO just insert a new session
            win.Session = new Session {App = proc.App};
        }

        private async ValueTask WithApp(ProcessInfo proc)
        {
            var id = await proc.GetIdentification();
            // TODO find by id, if exists return that else insert new app
            proc.App = new App();
        }

        private async Task Work(CancellationToken stoppingToken)
        {
            _fgWinWatcher
                // group by window, until the window closes
                .GroupByUntil(
                    sw => sw.Window,
                    win => win.Key.Closed)
                // group by process, until the process exits
                .GroupByUntil(
                    win => new ProcessInfo(win.Key),
                    proc => proc.Key.Exited)
                // set the app for the process
                .Do(async proc => await WithApp(proc.Key))
                // flatten the groups back
                .SelectMany(proc =>
                    proc
                        // set the session for the window
                        .Do(async win => await WithSession(win.Key, proc.Key))
                        // dispose of the process once the process exits
                        .Finally(() => proc.Key.Dispose())
                        .SelectMany(win =>
                            // dispose of the window once it closes
                            win.Finally(() => win.Key.Dispose())))
                // select every 2 switches
                .Buffer(2, 1)
                // and convert them to a usage
                .Select(sws => new ForegroundWindowUsage(sws[0], sws[1]))
                .Subscribe(proc =>
                {
                    _logger.LogInformation("{s} - {e} Switch `{win1}` ({app1}) to `{win2}` ({app2})",
                        proc.Start, proc.End,
                        proc.CurrentWindow.Title, proc.CurrentWindow.Session.App.Identification,
                        proc.NewWindow.Title, proc.NewWindow.Session.App.Identification);
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