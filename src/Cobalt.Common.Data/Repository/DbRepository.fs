namespace Cobalt.Common.Data.Repository

open Cobalt.Common.Data.Entities
open Cobalt.Common.Data.Migrations
open Microsoft.Data.Sqlite
open Dapper
open System.Linq

type IDbRepository = 
    abstract member Insert<'a> : 'a -> unit
    abstract member Delete<'a> : 'a -> unit
    abstract member Get<'a> : int64 -> 'a

type DbRepository (conn: SqliteConnection, mig: IMigrator) =
    let schema = mig.Migrate()

    let mapp = AppMaterializer(conn)
    let mtag = TagMaterializer(conn)

    let cmd sql = new SqliteCommand(sql, conn)
    let singleReader sql =
        let cmd = cmd sql
        let reader = cmd.ExecuteReader()
        reader.Read() |> ignore
        reader

    member _.insertSql<'a> () =
        let tbl = typeof<'a>.Name
        let flds =
            (schema.tables.Item tbl).fields.Select (fun x -> "@" + x.name)
            |> String.concat ","
        sprintf "insert into %s values (%s)" tbl flds

    interface IDbRepository with
        member x.Insert obj = 
            match box obj with
                | :? App as o ->
                    let c = cmd (x.insertSql<App>())
                    mapp.Dematerialize o c.Parameters
                    c.ExecuteNonQuery() |> ignore
                | :? Tag as o -> ()
                | :? Alert as o -> ()
                | _ -> failwithf "type %A not allowed for Insert" (obj.GetType())

        member x.Delete obj =
            match box obj with
                | :? Alert as x -> ()
                | _ -> failwithf "type %A not allowed for Delete" (obj.GetType())

        member x.Get<'a> id = 
            let reader = singleReader (sprintf "select * from %s where Id = %d" (typeof<'a>.Name) id)
            match typeof<'a> with
                | t when t = typeof<App> ->
                    box (mapp.Materialize 0 reader) :?> 'a
                | t when t = typeof<Tag> ->
                    box (mtag.Materialize 0 reader) :?> 'a
                | _ -> failwithf "type %A not allowed for Get" typeof<'a>
