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

    [<Fact>]
    member this.``Inserting app usage does not fail``() =
        let a = { Id = 0L; Name = "Name1"; Color = "black"; Path="C:\gg"; Icon=lazy [|32uy; 65uy; 45uy|]; Tags=null }
        repo.Insert a
        let au = { Id=0L;
            Start=DateTime.Now;
            End=DateTime.Now.AddDays(1.0);
            StartReason=AppUsageStartReason.Switch;
            EndReason=AppUsageEndReason.Switch;
            UsageType=AppUsageType.Focus;
            App=a}
        repo.Insert au

        let appId = conn.ExecuteScalar<int64>("select Id from App")
        let res = conn.Query<AppUsageObj>("select * from AppUsage").AsList();
        res |> should haveCount 1
        let au1 = res.[0]
        au1.Id |> should not' (equal 0L)
        au1.AppId |> should equal appId
        au1.Start |> should equal au.Start.Ticks
        au1.End |> should equal au.End.Ticks
        au1.UsageType |> should equal 0L

    [<Fact>]
    member this.``Inserting reminder does not fail``() =
        let r1 = { Id=0L; Offset=TimeSpan.FromHours(5.0); Action=ReminderAction.Script("rm -rf /")}
        let r2 = { Id=0L; Offset=TimeSpan.FromHours(0.0); Action=ReminderAction.Warn }
        let r3 = { Id=0L; Offset=TimeSpan.FromHours(3.0); Action=ReminderAction.CustomWarn("wut") }
        repo.Insert r1
        repo.Insert r2
        repo.Insert r3

        let res = conn.Query<ReminderObj>("select * from Reminder")
        res |> should haveCount 3
        let [q1;q2;q3] = List.ofSeq res
        q1.Id |> should not' (equal 0L)
        q1.Offset |> should equal r1.Offset.Ticks
        q1.ActionType |> should equal 2L
        q1.ActionParam |> should equal "rm -rf /"
        q2.Id |> should not' (equal 0L)
        q2.Offset |> should equal r2.Offset.Ticks
        q2.ActionType |> should equal 0L
        q2.ActionParam |> should equal null
        q3.Id |> should not' (equal 0L)
        q3.Offset |> should equal r3.Offset.Ticks
        q3.ActionType |> should equal 1L
        q3.ActionParam |> should equal "wut"



        
    interface IDisposable with
        member this.Dispose() = 
            conn.Close()
            File.Delete("data.db")
