namespace Common.Common.Data.Migrations

open System.Data.SQLite
open Dapper

type SQLiteMigrator(conn: SQLiteConnection) =
    let migrations:IDbMigration[] = [|new SQLiteMigrationV1(conn)|]
    member this.GetVersion()=
        try
            conn.ExecuteScalar<int64>("select CurrentVersion from MigrationInfo")
        with
        | :? SQLiteException as e when e.Message.Contains("no such table: MigrationInfo") -> 0L
    interface IDbMigrator with
        member this.Migrations = migrations
        member this.Migrate(): unit=
            let ver = this.GetVersion()
            migrations
            |> Seq.filter (fun x -> x.Version > ver)
            |> Seq.sortBy (fun x -> x.Version)
            |> Seq.iter (fun x -> x.Run())