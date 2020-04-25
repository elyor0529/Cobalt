using Cobalt.Common.Communication;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Cobalt.Engine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Trace); })
                .ConfigureKestrel(opts =>
                {
                    opts.ListenLocalhost(CommunicationManager.Port,
                        lstOpts => { lstOpts.Protocols = HttpProtocols.Http2; });
                })
                .UseStartup<Startup>();
        }
    }
}