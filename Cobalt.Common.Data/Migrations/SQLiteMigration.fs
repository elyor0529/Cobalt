namespace Common.Common.Data.Migrations

open System.Data.SQLite
open Dapper
open System.Text
open System

[<AbstractClass>]
type SQLiteMigration(conn: SQLiteConnection, version) =
    let _fld (name: string) (typ: string) (rest: string) =
        sprintf "%s %s %s" name typ rest 
    let _table (name: string) (fields: string list) =
        sprintf "create table %s (%s)" name (String.Join(",", fields))
    let _exec (sql: string list) =
        conn.Execute(String.Join(";", sql)) |> ignore
    let _insert name (flds:string list) =
        sprintf "insert into %s values (%s)" name (String.Join(",", flds))

    member x.exec = _exec
    member x.table = _table
    member x.fldr = _fld
    member x.fld n t = _fld n t ""
    member x.pkLong n = _fld n "integer" "primary key autoincrement"
    member x.fldLong n = _fld n "integer" ""
    member x.fldStr n = _fld n "text" ""
    member x.fldBlob n = _fld n "blob" ""
    member x.insert = _insert

    abstract member MigrateRun: unit -> unit
    interface IDbMigration with
        member this.Run(): unit = 
            this.MigrateRun()
        member this.Version = version


