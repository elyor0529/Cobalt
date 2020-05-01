using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;

namespace Cobalt.Engine
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<EngineService>();

            services.AddCodeFirstGrpc(config => { config.ResponseCompressionLevel = CompressionLevel.Optimal; });

            services.AddHostedService<EngineWorker>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapGrpcService<EngineService>(); });
        }
    }
}