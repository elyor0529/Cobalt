module Data

open Xunit
open Swensen.Unquote
open System.Collections
open System.Collections.Generic
open System.Linq
open Cobalt.Common.Data.Entities
open Cobalt.Common.Data.Migrations
open Cobalt.Common.Data.Migrations.Meta
open Microsoft.Data.Sqlite
open System.Data
open Dapper
open Cobalt.Common.Data.Repository
open System.IO
open System
open Microsoft.Extensions.Logging.Abstractions

let getTestConn () = 
    let rng = new Random()
    let ret = new SqliteConnection(sprintf "Data Source=:memory:")
    ret.Open()
    ret

[<Fact>]
let ``migration 1`` () = 
    let conn = getTestConn()
    let migration = Migration1(conn)
    let ctx = SchemaContext emptySchema
    migration.DescribeMigration ctx
    test <@ ctx.Changes.table.altered = List.empty @>
    test <@ ctx.Changes.table.removed = List.empty @>
    test <@ ctx.Changes.table.added <> List.empty @>

    migration.Migrate emptySchema
    let count = conn.ExecuteScalar("select count(*) from Alert") :?> int64
    test <@ count = 0L @>

let toArray (stream: Stream) = 
    match stream with
        | :? MemoryStream -> (stream :?> MemoryStream).ToArray()
        | _ -> 
            use mem = new MemoryStream()
            stream.CopyTo(mem)
            mem.ToArray()

let readBlob conn tbl col id =
    toArray (new SqliteBlob(conn, tbl, col, id))


type Repository () = 
    let conn = getTestConn()
    let mig = Migrator(conn, NullLogger<Migrator>()) :> IMigrator
    let repo = new DbRepository(conn, mig, NullLogger<DbRepository>()) :> IDbRepository


    [<Fact>]
    let ``connection success`` () = 
        test <@ conn.ExecuteScalar("select max(version) from migration") :?> int64 = 1L @>

    [<Fact>]
    let ``insert app`` () =
        let app1 = { Id = 2L; Name = "App1"; Identification = UWP "Main"; Background = "black"; Icon = new MemoryStream([||]); Tags = null }
        let app2 = { Id = 3L; Name = "App2"; Identification = Win32 @"C:\Users\default\12.exe"; Background = "grey"; Icon = new MemoryStream([|1uy;2uy;3uy;4uy|]); Tags = null }
        let app3 = { Id = 5L; Name = "App3"; Identification = Java "GhidraClassLoader Ghidra"; Background = "black"; Icon = new MemoryStream([| 2uy |]); Tags = null }
        let rapp1 = repo.Insert app1
        let rapp2 = repo.Insert app2
        let rapp3 = repo.Insert app3

        test <@ toArray app1.Icon = toArray rapp1.Icon @>
        test <@ toArray app2.Icon = toArray rapp2.Icon @>
        test <@ toArray app3.Icon = toArray rapp3.Icon @>
        test <@ Seq.isEmpty rapp1.Tags.Value @>
        test <@ Seq.isEmpty rapp2.Tags.Value @>
        test <@ Seq.isEmpty rapp3.Tags.Value @>
        test <@ { app1 with Icon = null } = { rapp1 with Tags = null; Icon = null } @>
        test <@ { app2 with Icon = null } = { rapp2 with Tags = null; Icon = null } @>
        test <@ { app3 with Icon = null } = { rapp3 with Tags = null; Icon = null } @>

        let reader = conn.ExecuteReader "select * from App"
        test <@ reader.Read() = true @>
        test <@ reader.GetInt64(reader.GetOrdinal("Id")) = app1.Id @>
        test <@ reader.GetString(reader.GetOrdinal("Name")) = app1.Name @>
        test <@ reader.GetInt64(reader.GetOrdinal("Identification_Tag")) = 1L @>
        test <@ reader.GetString(reader.GetOrdinal("Identification_Text1")) = "Main" @>
        test <@ reader.GetString(reader.GetOrdinal("Background")) = app1.Background @>
        test <@ readBlob conn "App" "Icon" app1.Id = [||] @>
        test <@ reader.Read() = true @>
        test <@ reader.GetInt64(reader.GetOrdinal("Id")) = app2.Id @>
        test <@ reader.GetString(reader.GetOrdinal("Name")) = app2.Name @>
        test <@ reader.GetInt64(reader.GetOrdinal("Identification_Tag")) = 0L @>
        test <@ reader.GetString(reader.GetOrdinal("Identification_Text1")) = @"C:\Users\default\12.exe" @>
        test <@ reader.GetString(reader.GetOrdinal("Background")) = app2.Background @>
        test <@ readBlob conn "App" "Icon" app2.Id = [|1uy; 2uy; 3uy; 4uy;|] @>
        test <@ reader.Read() = true @>
        test <@ reader.GetInt64(reader.GetOrdinal("Id")) = app3.Id @>
        test <@ reader.GetString(reader.GetOrdinal("Name")) = app3.Name @>
        test <@ reader.GetInt64(reader.GetOrdinal("Identification_Tag")) = 2L @>
        test <@ reader.GetString(reader.GetOrdinal("Identification_Text1")) = @"GhidraClassLoader Ghidra" @>
        test <@ reader.GetString(reader.GetOrdinal("Background")) = app3.Background @>
        test <@ readBlob conn "App" "Icon" app3.Id = [| 2uy |] @>
        test <@ reader.Read() = false @>

    [<Fact>]
    let ``insert app and read it back`` () =
        let app1 = { Id = 2L; Name = "App1"; Identification = UWP "Main"; Background = "black"; Icon = new MemoryStream([||]); Tags = null }
        let app2 = { Id = 3L; Name = "App2"; Identification = Win32 @"C:\Users\default\12.exe"; Background = "grey"; Icon = new MemoryStream([|1uy;2uy;3uy;4uy|]); Tags = null }
        let app3 = { Id = 5L; Name = "App3"; Identification = Java "GhidraClassLoader Ghidra"; Background = "black"; Icon = new MemoryStream([| 2uy |]); Tags = null }
        repo.Insert app1 |> ignore
        repo.Insert app2 |> ignore
        repo.Insert app3 |> ignore

        let rapp1 = repo.Get<App> app1.Id
        let rapp2 = repo.Get<App> app2.Id
        let rapp3 = repo.Get<App> app3.Id

        test <@ toArray rapp1.Icon  = [||] @>
        test <@ toArray rapp2.Icon  = [|1uy; 2uy; 3uy; 4uy|] @>
        test <@ toArray rapp3.Icon  = [|2uy|] @>

        test <@ { app1 with Icon = null; } = { rapp1 with Icon = null; Tags = null } @>
        test <@ { app2 with Icon = null } = { rapp2 with Icon = null; Tags = null } @>
        test <@ { app3 with Icon = null } = { rapp3 with Icon = null; Tags = null } @>

    [<Fact>]
    let ``insert tag and read it back`` () =
        let tag1 = { Id = 2L; Name = "Tag1"; Color = "blue"; Apps = null }
        repo.Insert tag1 |> ignore

        let rtag1 = repo.Get<Tag> tag1.Id
        test <@ tag1 = { rtag1 with Apps = null } @>

    [<Fact>]
    let ``inserted object with Id = 0 gets a new Id when inserted`` () =
        let tag1 = { Id = 0L; Name = "Tag1"; Color = "blue"; Apps = null }
        let itag1 = repo.Insert tag1
        test <@ itag1.Id <> 0L @>

        let rtag1 = conn.Query<Tag> "select * from tag"
        test <@ rtag1.Single().Id <> 0L @>

        let tag2 = { Id = 0L; Name = "Tag2"; Color = "red"; Apps = null }
        let itag2 = repo.Insert tag2
        test <@ itag2.Id = 2L @>

        let all = conn.Query<Tag> "select * from tag"
        test <@ all.Count() = 2 @>

        let rtag2 = conn.Query<Tag> "select * from tag where Color = 'red'"
        test <@ rtag2.Single().Id = 2L @>

    [<Fact>]
    let ``add and remove tag to app`` () =
        let app1 = { Id = 2L; Name = "App1"; Identification = UWP "Main"; Background = "black"; Icon = new MemoryStream([||]); Tags = null }
        let tag1 = { Id = 1L; Name = "Tag1"; Color = "blue"; Apps = null }
        repo.Insert app1 |> ignore
        repo.Insert tag1 |> ignore

        repo.InsertTagToApp app1 tag1

        let rapp1 = repo.Get<App> app1.Id
        let col = rapp1.Tags.Value;
        test <@ { col.Single() with Apps = null } = tag1 @>

        repo.DeleteTagToApp app1 tag1
        let rapp2 = repo.Get<App> app1.Id
        test <@ rapp2.Tags.Value.Count() = 0 @>


    [<Fact>]
    let ``insert session`` () =
        let app1 = { Id = 3L; Name = "App2"; Identification = Win32 @"C:\Users\default\12.exe"; Background = "grey"; Icon = new MemoryStream([|1uy;2uy;3uy;4uy|]); Tags = null }
        let sess1 = { Id = 0L; Title = "Window1"; CmdLine = "youface.exe boobies"; App = app1 }

        let rapp1 = repo.Insert app1
        let rsess1 = repo.Insert sess1
        test <@ { rsess1 with Id = 0L } = sess1 @>

        let rsess1 = repo.Get<Session> rsess1.Id
        test <@ { sess1 with App = app1; Id = 0L } = { rsess1 with App = app1; Id = 0L } @>
        let rapp2 = rsess1.App
        test <@ rapp2.Id = app1.Id @>

    [<Fact>]
    let ``insert usage`` () =
        let now = DateTime.Now
        let app1 = { Id = 3L; Name = "App2"; Identification = Win32 @"C:\Users\default\12.exe"; Background = "grey"; Icon = new MemoryStream([|1uy;2uy;3uy;4uy|]); Tags = null }
        let sess1 = { Id = 2L; Title = "Window1"; CmdLine = "youface.exe boobies"; App = app1 }
        let usage1 = { Id = 1L; Start = now.AddHours(1.0); End = now.AddHours(2.0); Session = sess1 }

        let rapp1 = repo.Insert app1
        let rsess1 = repo.Insert sess1
        let rusage1 = repo.Insert usage1
        test <@ { rusage1 with Session=sess1 } = {usage1 with Session=sess1} @>

        let rusage1 = repo.Get<Usage> rusage1.Id
        test <@ { rusage1 with Session =sess1} = { usage1 with Session = sess1; } @>
        let sess2 = rusage1.Session
        test <@ sess2.Id = sess1.Id @>

    [<Fact>]
    let ``insert se`` () =
        let now = DateTime.Now
        let se1 = { Id = 0L; Timestamp = now; Kind = SystemEventKind.Idle }

        let rse1 = repo.Insert se1
        test <@ { rse1 with Id = 0L } = se1 @>

        let rse1 = repo.Get<SystemEvent> rse1.Id
        test <@ { rse1 with Id = 0L } = se1 @>

    [<Fact>]
    let ``insert alert`` () =
        let now = DateTime.Now
        let app1 = { Id = 3L; Name = "App2"; Identification = Win32 @"C:\Users\default\12.exe"; Background = "grey"; Icon = new MemoryStream([|1uy;2uy;3uy;4uy|]); Tags = null }
        let tag1 = { Id = 1L; Name = "Tag1"; Color = "blue"; Apps = null }
        let alt1 = { Id = 0L; Target = App app1; TimeRange = Once (now.AddHours(5.0), now.AddHours(10.0)); UsageLimit = TimeSpan.FromHours(8.0); ExceededReaction = Kill }
        let alt2 = { Id = 0L; Target = Tag tag1; TimeRange = Repeated (RepeatType.Monthly, now.TimeOfDay, now.TimeOfDay.Add(TimeSpan(2,0,0))); UsageLimit = TimeSpan.FromHours(2.0); ExceededReaction = Message "boobies?" }

        let rapp1 = repo.Insert app1
        let rtag1 = repo.Insert tag1
        let alt1 = { alt1 with Target = App rapp1 }
        let alt2 = { alt2 with Target = Tag rtag1 }
        let ralt1 = repo.Insert alt1
        let ralt2 = repo.Insert alt2

        let ralt1 = repo.Get<Alert> ralt1.Id
        let ralt2 = repo.Get<Alert> ralt2.Id
        let ralt1id = match ralt1.Target with | Tag t -> t.Id; | App a -> a.Id
        let ralt2id = match ralt2.Target with | Tag t -> t.Id; | App a -> a.Id
        test <@ rapp1.Id = ralt1id @>
        test <@ rtag1.Id = ralt2id @>
        test <@ { ralt1 with Target = App app1; Id = 0L } = { alt1 with Target = App app1; Id = 0L } @>
        test <@ { ralt2 with Target = Tag tag1; Id = 0L } = { alt2 with Target = Tag tag1; Id = 0L } @>


    interface IDisposable with
        member _.Dispose () = 
            let src = conn.DataSource
            repo.Dispose()
            if not <| String.IsNullOrEmpty(src) then
                File.Delete(src)
