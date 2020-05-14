using System.IO.Compression;
using Cobalt.Common.Data.Migrations;
using Cobalt.Common.Data.Repository;
using Cobalt.Engine.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;

namespace Cobalt.Engine
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // TODO shift IoC stuff to a Common dll
            var connBuilder = new SqliteConnectionStringBuilder {DataSource = "data.db", ForeignKeys = true};
            var conn = new SqliteConnection(connBuilder.ToString());
            conn.Open();

            services.AddSingleton(conn);
            services.AddSingleton<IMigrator, Migrator>();
            services.AddSingleton<IDbRepository, DbRepository>();

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