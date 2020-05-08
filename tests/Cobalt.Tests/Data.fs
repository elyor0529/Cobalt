module Data

open Xunit
open Swensen.Unquote
open Microsoft.FSharp.Reflection
open Cobalt.Common.Data.Entities
open Cobalt.Common.Data.Migrations
open Cobalt.Common.Data.Migrations.Meta
open Microsoft.Data.Sqlite
open System.Data
open Dapper

[<Fact>]
let ``migration 1`` () = 
    use conn = new SqliteConnection("Data Source=:memory:")
    conn.Open()
    let migration = Migration1(conn)
    let ctx = SchemaContext emptySchema
    migration.DescribeMigration ctx
    test <@ ctx.Changes.table.altered = List.empty @>
    test <@ ctx.Changes.table.removed = List.empty @>
    test <@ ctx.Changes.table.added <> List.empty @>

    migration.Migrate emptySchema
    let count = conn.ExecuteScalar("select count(*) from Alert") :?> int64
    test <@ count = 0L @>

[<Fact>]
let ``connection success`` () = 
    use conn = new SqliteConnection("Data Source=:memory:")
    conn.Open()
    let mig = Migrator(conn) :> IMigrator

    let sch = mig.Migrate()
    test <@ conn.ExecuteScalar("select max(version) from migration") :?> int64 = 1L @>
    test <@ not sch.tables.IsEmpty @>
    test <@ not sch.indexes.IsEmpty @>
