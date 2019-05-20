﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using Cobalt.Common.IoC;
using Microsoft.Win32.TaskScheduler;

namespace Cobalt.Tests.Integration
{
    public enum TaskName
    {
        Cobalt,
        CobaltEngine,
        CobaltAlerts,
        CobaltNotifier
    }

    public class TaskServiceUtil : IDisposable
    {

        public TaskService TaskService { get; }
        public string InstallLocation { get; }

        public List<Task> Tasks { get; }

        public TaskServiceUtil(params TaskName[] names)
        {
            { 
                var includeTypes = new [] { typeof(Engine.Program), typeof(Cobalt.App), typeof(Cobalt.TaskbarNotifier.App) };
            }
            Tasks = new List<Task>();
            TaskService = new TaskService();
            InstallLocation = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Engine.Program)).Location);
            Console.WriteLine(InstallLocation);
            //InstallLocation = Path.GetDirectoryName(@"F:\Code\Personal\Cobalt\Cobalt.Tests\bin\Debug\");
            foreach (var taskName in names)
            {
                var strName = ToStringName(taskName);
                var task = InstallTask(strName);
                Tasks.Add(task);
                LaunchTask(task);
            }
        }

        private Task InstallTask(string prog)
        {
            var task = TaskService.NewTask();

            task.Triggers.Add(new LogonTrigger());
            task.Actions.Add(
                new ExecAction($"{InstallLocation}\\{prog}.exe", "", InstallLocation));

            task.Principal.RunLevel = TaskRunLevel.Highest;
            task.Principal.LogonType = TaskLogonType.InteractiveToken;
            //users group
            task.Principal.GroupId = "S-1-5-32-545";

            task.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
            task.Settings.DisallowStartIfOnBatteries = false;
            task.Settings.StopIfGoingOnBatteries = false;
            task.Settings.AllowHardTerminate = false;
            task.Settings.StartWhenAvailable = true;
            task.Settings.RunOnlyIfNetworkAvailable = false;
            task.Settings.IdleSettings.StopOnIdleEnd = false;
            task.Settings.IdleSettings.RestartOnIdle = false;
            task.Settings.AllowDemandStart = true;
            task.Settings.Hidden = false;
            task.Settings.Enabled = true;
            task.Settings.RunOnlyIfIdle = false;
            task.Settings.WakeToRun = false;
            task.Settings.Priority = ProcessPriorityClass.Normal;

            return TaskService.RootFolder.RegisterTaskDefinition(prog + ".Test", task);
        }

        private void DeleteTask(Task task)
        {
            task.Folder.DeleteTask(task.Name);
        }

        private void StopTask(Task task)
        {
            //Util.StopCobalt();
            task.Stop();
        }

        private void LaunchTask(Task task)
        {
            task.Run();
        }

        private static string ToStringName(TaskName task)
        {
            switch (task)
            {
                case TaskName.Cobalt:
                    return "Cobalt";
                case TaskName.CobaltAlerts:
                    return "Cobalt.Alerts";
                case TaskName.CobaltEngine:
                    return "Cobalt.Engine";
                case TaskName.CobaltNotifier:
                    return "Cobalt.Notifier";
                default:
                    throw new Exception();
            }
        }

        public void Dispose()
        {
            foreach (var task in Tasks)
            {
                StopTask(task);
                DeleteTask(task);
            }
        }
    }
}