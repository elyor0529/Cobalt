module Data

open Xunit
open Swensen.Unquote
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
    let count = conn.ExecuteScalar("select count(*) from Alerts") :?> int64
    test <@ count = 0L @>

[<Fact>]
let ``conection success`` () = 
    use conn = new SqliteConnection("Data Source=:memory:")
    conn.Open()
    test <@ conn.State.HasFlag ConnectionState.Open @>

