using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cobalt.Common.Communication.Messages;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Data.Repository;
using Cobalt.Engine.Infos;
using Cobalt.Engine.Native;
using Cobalt.Engine.Watchers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;

namespace Cobalt.Engine.Services
{
    public class WatchEngine : BackgroundService
    {
        private readonly UsageService _engineSvc;
        private readonly ILogger<WatchEngine> _logger;
        private readonly IDbRepository _repo;
        private readonly WatchLoop _watchLoop;

        public WatchEngine(ILogger<WatchEngine> logger, UsageService engineSvc, IDbRepository repo)
        {
            _logger = logger;
            _engineSvc = engineSvc;
            _watchLoop = new WatchLoop();

            _repo = repo;
        }

        private App FindOrCreateApp(Process proc)
        {
            var id = proc.GetIdentification(proc);
            var app = _repo.FindAppByIdentification(id);

            return ValueOption.ToObj(app) ?? _repo.Insert(new App
            {
                Identification = id,
                Background = "#FEFEFE", // TODO
                Icon = new MemoryStream(), // TODO
                Name = "" // TODO
            });
        }

        private Session CreateSession(Window win, Process process, App app)
        {
            return _repo.Insert(new Session
            {
                App = app,
                Title = win.Title,
                CmdLine = process.CmdLine
            });
        }

        private async Task Work(CancellationToken stoppingToken)
        {
            var grfffes = What.add();
            using var usages = Window.ForegroundWatcher
                // group by window, until the window closes
                .GroupByUntil(
                    fgSwitch => fgSwitch.Window,
                    switchesPerWindow => switchesPerWindow.Key.Closed)
                // group by process, until the process exits
                .GroupByUntil(
                    switchesPerWindow => switchesPerWindow.Key.Process,
                    windowsPerProcess => windowsPerProcess.Key.Exited)
                // flatten the groups back
                .SelectMany(windowsPerProcess =>
                {
                    var process = windowsPerProcess.Key;
                    var app = FindOrCreateApp(process);
                    return windowsPerProcess
                        // dispose of the process once the process exits
                        .Finally(() => process.Dispose())
                        .SelectMany(switchesPerWindow =>
                        {
                            var window = switchesPerWindow.Key;
                            var session = CreateSession(window, process, app);
                            return switchesPerWindow
                                // dispose of the window once it closes
                                .Finally(() => window.Dispose())
                                .Select(sw => new { Timestamp = sw.Timestamp, App = app, Session = session });
                        });
                })
                // select every 2 switches
                .Buffer(2, 1)
                // and convert them to a usage
                .Select(sws =>
                {
                    var switch1 = sws[0];
                    var switch2 = sws[1];
                    return new
                    {
                        Start = switch1.Timestamp,
                        End = switch2.Timestamp,
                        Current = new
                        {
                            switch1.Session, switch1.App
                        },
                        New = new
                        {
                            switch2.Session, switch2.App
                        }
                    };
                })
                .Subscribe(fgSwitch =>
                {
                    var usage = _repo.Insert(new Usage
                    {
                        Start = fgSwitch.Start,
                        End = fgSwitch.End,
                        Session = fgSwitch.Current.Session
                    });
                    _engineSvc.PushUsageSwitch(new UsageSwitch
                    {
                        AppId = usage.Session.App.Id, SessionId = usage.Session.Id, UsageId = usage.Id,
                        NewSessionId = fgSwitch.New.Session.Id, NewAppId = fgSwitch.New.Session.App.Id
                    });
                    _logger.LogDebug(
                        "[SWITCH ({Start} : {End}) = {Duration}]\n`{CurrentSessionTitle}`\n({CurrentApp})\n\nto\n`{NewSessionTitle}`\n({NewApp})",
                        fgSwitch.Start, fgSwitch.End, fgSwitch.End - fgSwitch.Start,
                        fgSwitch.Current.Session.Title, fgSwitch.Current.Session.App, fgSwitch.New.Session.Title,
                        fgSwitch.New.Session.App);
                });

            await _watchLoop.Run(stoppingToken);
        }

        public override void Dispose()
        {
            _repo.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () => await Work(stoppingToken), stoppingToken);
        }
    }
}