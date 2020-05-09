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

let getTestConn () = 
    let ret = new SqliteConnection("Data Source=:memory:")
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
    let mig = Migrator(conn) :> IMigrator
    let repo = DbRepository(conn, mig) :> IDbRepository


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


    interface IDisposable with
        member _.Dispose () = 
            conn.Dispose()
