﻿using System;
using System.Data.SQLite;
using Autofac;
using Cobalt.Common.Data.Repository;
using Cobalt.Common.Transmission;

namespace Cobalt.Common.IoC
{
    public class IoCService
    {
        private static IoCService _instance;

        public IoCService()
        {
            var builder = new ContainerBuilder();
            RegisterDependencies(builder);
            Container = builder.Build();
        }

        public IoCService(IContainer container)
        {
            Container = container;
        }

        public IContainer Container { get; }

        public static IoCService Instance
        {
            get { return _instance = _instance ?? new IoCService(); }
            set => _instance = value;
        }

        public static void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies())
                .Where(type => type.Name.StartsWith("Cobalt"))
                .PreserveExistingDefaults()
                .AsSelf()
                //but we also make interfaces to be implemented
                .AsImplementedInterfaces()
                .InstancePerDependency();

            builder
                .Register(c => new SQLiteConnection("Data Source=dat.db"))
                .As<SQLiteConnection>()
                .InstancePerLifetimeScope();

            builder
                .RegisterType<SqliteRepository>()
                .As<IDbRepository>()
                .InstancePerLifetimeScope();

            //register single instance transmission client
            builder.RegisterInstance(new TransmissionClient())
                .As<ITransmissionClient>();
        }
    }
}