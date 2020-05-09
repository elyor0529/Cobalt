using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Attributes;
using Cobalt.Common.Data.Entities;
using Cobalt.Common.Data.Migrations;
using Cobalt.Common.Data.Repository;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Cobalt.Tests.Benchmarks
{
    [MemoryDiagnoser]
    public class InsertAppBenchmarks
    {
        private static readonly Random rng = new Random();
        private readonly string file = "testfile.db" + rng.Next();

        private SqliteCommand cmd;

        private int i;
        private SqliteConnection Connection { get; set; }
        private IDbRepository Repository { get; set; }

        private App app1 => new App
        {
            Id = 0L,
            Name = "Chrome",
            Background = "#fefefe",
            Icon = new MemoryStream(),
            Identification = AppIdentification.NewWin32("C:\\Desktop\\dumb_file.txt" + i++)
        };


        [GlobalSetup]
        public void OpenConnection()
        {
            var builder = new SqliteConnectionStringBuilder();
            builder.DataSource = file;
            builder.ForeignKeys = true;
            Connection = new SqliteConnection(builder.ToString());
            Connection.Open();
            var migrations = new Migrator(Connection);
            Repository = new DbRepository(Connection, migrations);
            cmd = new SqliteCommand(
                "insert into App(Name, Identification_Tag, Identification_Text1, Background, Icon) values (@Name, @Identification_Tag, @Identification_Text1, @Background, @Icon); select last_insert_rowid()",
                Connection);
            cmd.Prepare();
        }

        [GlobalCleanup]
        public void Close()
        {
            Connection.Close();
            File.Delete(file);
        }

        [Benchmark(Baseline = true)]
        public void AddAppUsingRaw2()
        {
            var app = app1;
            var text1 = app.Identification switch
            {
                AppIdentification.UWP x => x.PRAID,
                AppIdentification.Win32 x => x.Path,
                AppIdentification.Java x => x.MainJar,
                _ => throw new NotImplementedException()
            };
            cmd.Parameters.Clear();
            cmd.Parameters.AddRange(new[]
            {
                new SqliteParameter("Name", app.Name),
                new SqliteParameter("Identification_Tag", app.Identification.Tag),
                new SqliteParameter("Identification_Text1", text1),
                new SqliteParameter("Background", app.Background),
                new SqliteParameter("Icon", ((MemoryStream) app.Icon).ToArray())
            });
            app.Id = (long) cmd.ExecuteScalar();
            app.Icon = new SqliteBlob(Connection, "App", "Icon", app.Id);
            app.Tags = new Lazy<IEnumerable<Tag>>(); // needs more work to be realistic 
            Trace.Assert(app.Id != 0);
        }

        [Benchmark]
        public void AddAppUsingDapper()
        {
            var app = app1;
            var text1 = app.Identification switch
            {
                AppIdentification.UWP x => x.PRAID,
                AppIdentification.Win32 x => x.Path,
                AppIdentification.Java x => x.MainJar,
                _ => throw new NotImplementedException()
            };
            app.Id = Connection.ExecuteScalar<long>(
                "insert into App(Name, Identification_Tag, Identification_Text1, Background, Icon) values (@Name, @Identification_Tag, @Identification_Text1, @Background, @Icon); select last_insert_rowid()",
                new
                {
                    app.Name,
                    app.Background,
                    Identification_Tag = app.Identification.Tag, Identification_Text1 = text1,
                    Icon = ((MemoryStream) app.Icon).ToArray()
                });
            app.Icon = new SqliteBlob(Connection, "App", "Icon", app.Id);
            app.Tags = new Lazy<IEnumerable<Tag>>(); // needs more work to be realistic 
            Trace.Assert(app.Id != 0);
        }

        [Benchmark]
        public void AddAppUsingRepo()
        {
            var app = app1;
            app = Repository.Insert(app);
            Trace.Assert(app.Id != 0);
        }
    }
}