using System;
using System.Reflection;
using Cobalt.Common.Data.Migrations;
using Cobalt.Common.Data.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;

namespace Cobalt.Common.Infrastructure
{
    public static class ServiceExtensions
    {
        static ServiceExtensions()
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            Log.Logger = new LoggerConfiguration()
                // TODO read this from appsettings.json, default should be above Information
                // TODO override certain messages so that they are not so noisy e.g. Asp.Net/Grpc setup
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(new RenderedCompactJsonFormatter(), $"{assemblyName ?? "DefaultCobalt"}.log.txt")
                .CreateLogger();
        }

        public static IServiceCollection AddCobaltCommon(this IServiceCollection services)
        {
            return services.AddCobaltData();
        }

        public static IServiceCollection AddCobaltData(this IServiceCollection services)
        {
            var connectionBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = "data.db", // TODO shift this to LocalData
                ForeignKeys = true
            };
            var connection = new SqliteConnection(connectionBuilder.ToString());
            connection.Open();

            services.AddSingleton(connection);
            services.AddSingleton<IMigrator, Migrator>();
            services.AddSingleton<IDbRepository, DbRepository>();
            return services;
        }
    }
}
