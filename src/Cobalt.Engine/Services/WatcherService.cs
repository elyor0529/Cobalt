﻿using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Common.Communication.Messages;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Data.Repository;
using Cobalt.Engine.Infos;
using Cobalt.Engine.Watchers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;

namespace Cobalt.Engine.Services
{
    public class WatcherService : BackgroundService
    {
        private readonly UsageService _engineSvc;
        private readonly ForegroundWindowWatcher _fgWinWatcher;
        private readonly ILogger<WatcherService> _logger;
        private readonly IDbRepository _repo;
        private readonly WatchLoop _watchLoop;

        public WatcherService(ILogger<WatcherService> logger, UsageService engineSvc, IDbRepository repo)
        {
            _logger = logger;
            _engineSvc = engineSvc;
            _watchLoop = new WatchLoop();
            _fgWinWatcher = new ForegroundWindowWatcher();

            _repo = repo;
        }

        private ValueTask WithSession(WindowInfo win, ProcessInfo proc)
        {
            win.Session = _repo.Insert(new Session
            {
                App = proc.App,
                Title = win.Title,
                CmdLine = "not implemented" // TODO
            });
            return new ValueTask();
        }

        private async ValueTask WithApp(ProcessInfo proc)
        {
            var id = await proc.GetIdentification();
            proc.App = ValueOption.ToObj(_repo.FindAppByIdentification(id)) ?? _repo.Insert(new App
            {
                Identification = id,
                Background = "#FEFEFE", // TODO
                Icon = new MemoryStream(), // TODO
                Name = "" // TODO
            });
        }

        private async Task Work(CancellationToken stoppingToken)
        {
            using var usages = _fgWinWatcher
                // group by window, until the window closes
                .GroupByUntil(
                    sw => new WindowInfo(sw.Window),
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
                            win.Finally(() => win.Key.Dispose())
                                .Select(sw => new {sw.ActivatedTimestamp, Window = win.Key})))
                // select every 2 switches
                .Buffer(2, 1)
                // and convert them to a usage
                .Select(sws => new
                {
                    Start = sws[0].ActivatedTimestamp,
                    End = sws[1].ActivatedTimestamp,
                    CurrentWindow = sws[0].Window,
                    NewWindow = sws[1].Window
                })
                .Subscribe(fgUsage =>
                {
                    var usage = _repo.Insert(new Usage
                    {
                        Start = fgUsage.Start,
                        End = fgUsage.End,
                        Session = fgUsage.CurrentWindow.Session
                    });
                    _engineSvc.PushUsageSwitch(new UsageSwitch
                    {
                        AppId = usage.Session.App.Id, SessionId = usage.Session.Id, UsageId = usage.Id,
                        NewSessionId = fgUsage.NewWindow.Session.Id, NewAppId = fgUsage.NewWindow.Session.App.Id
                    });
                    _logger.LogDebug(
                        "[SWITCH ({Start} : {End}) = {Duration}]\n`{CurrentSessionTitle}`\n({CurrentApp})\n\nto\n`{NewSessionTitle}`\n({NewApp})",
                        fgUsage.Start, fgUsage.End, fgUsage.End - fgUsage.Start,
                        fgUsage.CurrentWindow.Title, fgUsage.CurrentWindow.Session.App, fgUsage.NewWindow.Title,
                        fgUsage.NewWindow.Session.App);
                });

            _fgWinWatcher.Watch();

            await _watchLoop.Run(stoppingToken);

            _fgWinWatcher.Dispose();
            _repo.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () => await Work(stoppingToken), stoppingToken);
        }
    }
}