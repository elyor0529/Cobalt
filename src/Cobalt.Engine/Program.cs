using System.IO.Compression;
using Cobalt.Common.Communication;
using Cobalt.Common.Infrastructure;
using Cobalt.Engine.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;
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
                .ConfigureServices(services =>
                {
                    services.AddCobaltCommon();

                    services.AddCodeFirstGrpc(config =>
                        config.ResponseCompressionLevel = CompressionLevel.Optimal);

                    services.AddSingleton<UsageService>();
                    services.AddHostedService<WatcherService>();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<UsageService>());
                })
                .ConfigureKestrel(opts =>
                {
                    opts.ListenLocalhost(CommunicationManager.Port,
                        lstOpts => { lstOpts.Protocols = HttpProtocols.Http2; });
                })
                .UseSerilog(dispose: true);
        }
    }
}