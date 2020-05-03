module Data

open Xunit
open Swensen.Unquote
open Cobalt.Common.Data.Migrations
open Cobalt.Common.Data.Migrations.Meta
open Microsoft.Data.Sqlite
open System.Data

[<Fact>]
let ``migration 1`` () = 
    let migration = Migration1()
    let ctx = SchemaContext emptySchema
    migration.Migrate ctx
    test <@ ctx.Changes.table.altered = List.empty @>
    test <@ ctx.Changes.table.removed = List.empty @>
    test <@ ctx.Changes.table.added <> List.empty @>

[<Fact>]
let ``conection success`` () = 
    use conn = new SqliteConnection("Data Source=:memory:")
    conn.Open()
    test <@ conn.State.HasFlag ConnectionState.Open @>

