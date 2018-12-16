namespace Cobalt.Tests

open System
open Xunit
open System.Data.SQLite
open System.IO
open Dapper
open Cobalt.Common.Data
open FsUnit
open FsUnit.Xunit
open Common.Common.Data.Migrations

type Data() =

    let conn = (new SQLiteConnection("Data Source=data.db")).OpenAndReturn()
    let mig = new SQLiteMigrator(conn)
    let repo = new SQLiteRepository(conn, mig) :> IDbRepository

    [<Fact>]
    member this.``Inserting app does not fail``() =
        let app = { Id = 0L; Name = "Name1"; Color = "black"; Path="C:\gg"; Icon=lazy [|32uy; 65uy; 45uy|]; Tags=null }
        repo.Insert app
        
        let res = conn.Query<AppObj>("select * from App").AsList()
        res |> should haveCount 1
        let obj = res.[0]
        obj.Name |> should equal "Name1"
        obj.Color |> should equal "black"
        obj.Path |> should equal "C:\gg"
        obj.Icon |> should equal [|32uy; 65uy; 45uy|]
        obj.Id |> should not' (equal 0L)

    [<Fact>]
    member this.``Inserting tag does not fail``() =
        let tag1 = { Id = 0L; Name = "tag1"; ForegroundColor="white"; BackgroundColor="red" }
        let tag2 = { Id = 0L; Name = "tag2"; ForegroundColor="blue"; BackgroundColor="yellow" }
        repo.Insert tag1
        repo.Insert tag2

        let res = conn.Query<Tag>("select * from Tag").AsList()
        res |> should haveCount 2
        let t1, t2 = res.[0], res.[1]
        t1.Name |> should equal "tag1"
        t1.ForegroundColor |> should equal "white"
        t1.BackgroundColor |> should equal "red"
        t1.Id |> should not' (equal 0L)
        t2.Name |> should equal "tag2"
        t2.ForegroundColor |> should equal "blue"
        t2.BackgroundColor |> should equal "yellow"
        t2.Id |> should not' (equal 0L)
        
    interface IDisposable with
        member this.Dispose() = 
            conn.Close()
            File.Delete("data.db")
