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
    public class InsertSessionBenchmarks
    {
        private static readonly Random rng = new Random();
        private readonly string file = "testfile.db" + rng.Next();

        private SqliteCommand cmd;
        private SqliteCommand cmd1;

        private int i;
        private SqliteConnection Connection { get; set; }
        private IDbRepository Repository { get; set; }

        private App app1 => new App
        {
            Id = 0L,
            Name = "Chrome",
            Background = "#fefefe",
            Icon = new MemoryStream(),
            Identification = AppIdentification.NewWin32("C:\\Desktop\\dumb_file.txt" + ++i)
        };

        private Session sess1 => new Session { Id = 0L, CmdLine = "tits.exe what", Title = "hacked", App = app1 };


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
            cmd1 = new SqliteCommand(
                "insert into Session(Title, CmdLine, AppId) values (@Title, @CmdLine, @AppId); select last_insert_rowid()",
                Connection);
            cmd1.Prepare();
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
            var sess = sess1;
            sess.App = app;
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

            cmd1.Parameters.Clear();
            cmd1.Parameters.AddRange(new[]
            {
                new SqliteParameter("Title", sess.Title),
                new SqliteParameter("CmdLine", sess.CmdLine),
                new SqliteParameter("AppId", sess.App.Id),
            });
            sess.Id = (long) cmd1.ExecuteScalar();

            Trace.Assert(app.Id != 0);
            Trace.Assert(sess.Id != 0);
        }

        [Benchmark]
        public void AddAppUsingDapper()
        {
            var app = app1;
            var sess = sess1;
            sess.App = app;
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
                    Identification_Tag = app.Identification.Tag, Identification_Text1 = text1 + ++i,
                    Icon = ((MemoryStream) app.Icon).ToArray()
                });
            app.Icon = new SqliteBlob(Connection, "App", "Icon", app.Id);
            app.Tags = new Lazy<IEnumerable<Tag>>(); // needs more work to be realistic 


            sess.Id = Connection.ExecuteScalar<long>(
                "insert into Session(Title, CmdLine, AppId) values (@Title, @CmdLine, @AppId); select last_insert_rowid()",
                new
                {
                    sess.Title,
                    sess.CmdLine,
                    AppId = sess.App.Id
                });

            Trace.Assert(app.Id != 0);
            Trace.Assert(sess.Id != 0);
        }

        [Benchmark]
        public void AddAppUsingRepo()
        {
            var app = app1;
            var sess = sess1;
            app = Repository.Insert(app);
            sess.App = app;
            sess = Repository.Insert(sess);
            Trace.Assert(app.Id != 0);
            Trace.Assert(sess.Id != 0);
        }
    }
}