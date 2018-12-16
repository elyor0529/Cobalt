module Data

open System
open Xunit
open System.Data.SQLite
open System.IO
open Dapper
open Cobalt.Common.Data
open FsUnit
open FsUnit.Xunit
open Common.Common.Data.Migrations

type Tests() =

    let conn = (new SQLiteConnection("Data Source=data.db")).OpenAndReturn()
    let mig = new SQLiteMigrator(conn)
    let repo = new SQLiteRepository(conn, mig) :> IDbRepository

    [<Fact>]
    member this.``Inserting app does not fail`` () =
        let app = { Id = 0L; Name = "Name1"; Color = "black"; Path="C:\gg"; Icon=lazy [|32uy; 65uy; 45uy|]; Tags=null }
        repo.Insert(app)
        
        let res = conn.Query<AppObj>("select * from App").AsList()
        res |> should haveCount 1
        let obj = res.[0]
        obj.Name |> should equal "Name1"
        obj.Color |> should equal "black"
        obj.Path |> should equal "C:\gg"
        obj.Icon |> should equal [|32uy; 65uy; 45uy|]
        
    interface IDisposable with
        member this.Dispose() = 
            conn.Close()
            File.Delete("data.db")
