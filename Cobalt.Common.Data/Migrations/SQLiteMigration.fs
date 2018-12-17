namespace Common.Common.Data.Migrations

open System.Data.SQLite
open Dapper
open System.Text
open System
open System.Collections.Generic

type SQLiteForeignKeyDefinition = {
    key: string
    fTable: string
    fKey: string
    triggers: IDictionary<string, string>
}

[<AbstractClass>]
type SQLiteMigration(conn: SQLiteConnection, version) =
    let _fld (name: string) (typ: string) (rest: string) =
        sprintf "%s %s %s" name typ rest 
    let _table (name: string) (fields: string list) =
        sprintf "create table %s (%s)" name (String.Join(",", fields))
    let _exec (sql: string list) =
        let finalSql = String.Join(";", sql)
        conn.Execute(finalSql) |> ignore
    let _insert name (flds:string list) =
        sprintf "insert into %s values (%s)" name (String.Join(",", flds))
    let _keys (s:string list) =
        sprintf "primary key (%s)" (String.Join(",", s))
    let _fk fkDef =
        sprintf " foreign key(%s) references %s(%s) %s " fkDef.key fkDef.fTable fkDef.fKey
            (String.Join(" ", fkDef.triggers |> Seq.map (fun x-> sprintf "on %s %s" x.Key x.Value)))
    let _index name tbl (fields:string list) =
        sprintf "create index %s on %s(%s)" name tbl (String.Join(",", fields))

    member x.exec = _exec
    member x.table = _table
    member x.fldr = _fld
    member x.fld n t = _fld n t ""
    member x.pkLong n = _fld n "integer" "primary key autoincrement"
    member x.fldLong n = _fld n "integer" ""
    member x.fldStr n = _fld n "text" ""
    member x.fldBlob n = _fld n "blob" ""
    member x.insert = _insert
    member x.keys = _keys
    member x.fk = _fk
    member x.index = _index

    abstract member MigrateRun: unit -> unit
    interface IDbMigration with
        member this.Run(): unit = 
            this.MigrateRun()
        member this.Version = version


