namespace Cobalt.Common.Data.Migrations

open Microsoft.Data.Sqlite
open Dapper
open Meta

type IMigrator =
    abstract member Migrate: unit -> Schema;

type Migrator (conn: SqliteConnection) = 
    let currentVer = 
        try 
            conn.ExecuteScalar<int64>("select max(version) from migration")
        with
        | :? SqliteException -> 0L
        |> int

    let migrations: MigrationBase list = [Migration1(conn)]

    let migrate sch (mig: MigrationBase) =
        mig.MigrateSchema sch;
        if mig.Version > currentVer then
            mig.Migrate sch.Schema;
        sch

    interface IMigrator with
        member _.Migrate() =
            let ctx =
                migrations
                    |> List.sortBy (fun x -> x.Version)
                    |> List.fold migrate (SchemaContext emptySchema)
            ctx.Schema

