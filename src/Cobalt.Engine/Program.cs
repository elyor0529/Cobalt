using System;
using Cobalt.Common.Communication;
using Cobalt.Common.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Cobalt.Engine
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            CreateHostBuilder(args).Build().RunWithLogging();
        }

        private static IWebHostBuilder CreateHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Trace); })
                .UseSerilog(dispose: true)
                .ConfigureKestrel(opts =>
                {
                    opts.ListenLocalhost(CommunicationManager.Port,
                        lstOpts => { lstOpts.Protocols = HttpProtocols.Http2; });
                })
                .UseStartup<Startup>();
        }
    }
}