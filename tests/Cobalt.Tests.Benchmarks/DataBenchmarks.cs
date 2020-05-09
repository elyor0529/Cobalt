using System;
using System.Collections.Generic;
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
    public class DataBenchmarks
    {
        private SqliteConnection Connection { get; set; }
        private static Random rng = new Random();
        private string file = "testfile.db" + rng.Next();
        private IDbRepository Repository { get; set; }

        private int i = 0;
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
            Connection = new SqliteConnection(builder.ConnectionString);
            Connection.Open();
            var migrations = new Migrator(Connection);
            Repository = new DbRepository(Connection, migrations);
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
                _ => throw new NotImplementedException(),
            };
            var cmd = new SqliteCommand("insert into App(Name, Identification_Tag, Identification_Text1, Background, Icon) values (@Name, @Identification_Tag, @Identification_Text1, @Background, @Icon); select last_insert_rowid()", Connection);
            cmd.Parameters.AddWithValue("Name", app.Name);
            cmd.Parameters.AddWithValue("Identification_Tag", app.Identification.Tag);
            cmd.Parameters.AddWithValue("Identification_Text1", text1);
            cmd.Parameters.AddWithValue("Background", app.Background);
            cmd.Parameters.AddWithValue("Icon", ((MemoryStream) app.Icon).ToArray());
            app.Id = (long)cmd.ExecuteScalar();
            app.Icon = new SqliteBlob(Connection, "App", "Icon", app.Id);
            app.Tags = new Lazy<IEnumerable<Tag>>(); // needs more work to be realistic 
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
                _ => throw new NotImplementedException(),
            };
            app.Id = Connection.ExecuteScalar<long>(
                "insert into App(Name, Identification_Tag, Identification_Text1, Background, Icon) values (@Name, @Identification_Tag, @Identification_Text1, @Background, @Icon); select last_insert_rowid()",
                new
                {
                    Name = app.Name, Background = app.Background,
                    Identification_Tag = app.Identification.Tag, Identification_Text1 = text1,
                    Icon = ((MemoryStream) app.Icon).ToArray()
                });
            app.Icon = new SqliteBlob(Connection, "App", "Icon", app.Id);
            app.Tags = new Lazy<IEnumerable<Tag>>(); // needs more work to be realistic 
        }

        [Benchmark]
        public void AddAppUsingRepo()
        {
            var app = app1;
            app = Repository.Insert(app);
        }

        public void InsertApp()
        {
            var app = new App
            {
                Id = 0L,
                Name = "Chrome",
                Background = "#fefefe",
                Icon = new MemoryStream(),
                Identification = AppIdentification.NewWin32("C:\\Desktop\\dumb_file.txt")
            };

            var alert = new Alert
            {
                Id = 1L,
                UsageLimit = TimeSpan.FromHours(4),
                ExceededReaction = Reaction.NewMessage("pls no more web browsing"),
                Target = Target.NewApp(app),
                TimeRange = TimeRange.NewRepeated(RepeatType.Monthly, TimeSpan.FromHours(08), TimeSpan.FromHours(18))
            };

            var perDay = alert.TimeRange switch
            {
                TimeRange.Once o => o.End - o.Start,
                TimeRange.Repeated r when r.Type == RepeatType.Weekly => (r.EndOfDay - r.StartOfDay) * 7,
                TimeRange.Repeated r => r.EndOfDay - r.StartOfDay,
                _ => throw new NotImplementedException()
            };

            var actions = alert.ExceededReaction switch
            {
                var x when x.IsKill => 1,
                Reaction.Message x => x.Message.Length,
                _ => throw new NotImplementedException()
            };
        }
    }
}
