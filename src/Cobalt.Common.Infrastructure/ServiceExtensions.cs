using System;
using Cobalt.Common.Data.Migrations;
using Cobalt.Common.Data.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Cobalt.Common.Infrastructure
{
    public static class ServiceExtensions
    {
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
