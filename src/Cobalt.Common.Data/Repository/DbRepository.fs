namespace Cobalt.Common.Data.Repository

open Cobalt.Common.Data.Entities
open Cobalt.Common.Data.Migrations
open Microsoft.Data.Sqlite
open Dapper
open System.Linq
open Cobalt.Common.Data.Migrations.Meta

type IDbRepository = 
    abstract member Insert<'a> : 'a -> 'a
    abstract member Delete<'a> : 'a -> unit
    abstract member Get<'a> : int64 -> 'a

    abstract member InsertTagToApp : App -> Tag -> unit 
    abstract member DeleteTagToApp : App -> Tag -> unit 

module RepoHelpers = 
    let inline insertSql< ^T when ^T: (member Id: int64)> (sch: Schema) (o: ^T) = 
        let tbl = sch.tables.Item typeof< ^T>.Name
        let flds = tbl.fields.Select (fun x -> x.name)
        let (tblCols, tblFlds) = 
            if (^T : (member Id: int64) (o)) = 0L then
                (flds
                    |> Seq.except ["Id"]
                    |> String.concat ","
                    |> sprintf "%s(%s)" tbl.name,
                flds
                    |> Seq.except ["Id"]
                    |> Seq.map ((+) "@")
                    |> String.concat ",")
            else
                (tbl.name,
                flds
                    |> Seq.map ((+) "@")
                    |> String.concat ",")
        sprintf "insert into %s values (%s); select last_insert_rowid()" tblCols tblFlds

open RepoHelpers

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
    let reader (sql: string) p fn = 
        let reader = conn.ExecuteReader(sql, p)
        seq { 
            while reader.Read() do
                yield fn reader
        }
    let exec sql p =
        conn.Execute(sql, p) |> ignore

    let tagsFor appId = 
        lazy reader "select * from tag where Id in (select TagId from App_Tag where AppId = @AppId)"
            {| AppId = appId |} (mtag.Materialize 0)
    let appsFor tagId = 
        lazy reader "select * from app where Id in (select AppId from App_Tag where TagId = @TagId)"
            {| TagId = tagId |} (mapp.Materialize 0)

    interface IDbRepository with
        member x.Insert obj = 
            match box obj with
                | :? App as o ->
                    let c = cmd (insertSql schema o)
                    mapp.Dematerialize o c.Parameters
                    let id = c.ExecuteScalar() :?> int64
                    box { o with
                            Id = id;
                            Icon = new SqliteBlob(conn, "App", "Icon", id);
                            Tags = tagsFor id
                        } :?> 'a
                | :? Tag as o ->
                    let c = cmd (insertSql schema o)
                    mtag.Dematerialize o c.Parameters
                    let id = c.ExecuteScalar() :?> int64
                    box { o with Id = id; Apps = appsFor id } :?> 'a
                | :? Alert as o -> box o :?> 'a
                | _ -> failwithf "type %A not allowed for Insert" (obj.GetType())

        member x.Delete obj =
            match box obj with
                | :? Alert as x -> ()
                | _ -> failwithf "type %A not allowed for Delete" (obj.GetType())

        member x.Get<'a> id = 
            let reader = singleReader (sprintf "select * from %s where Id = %d" (typeof<'a>.Name) id)
            match typeof<'a> with
                | t when t = typeof<App> ->
                    let app = mapp.Materialize 0 reader
                    let app = { app with Tags = tagsFor app.Id }
                    box app :?> 'a
                | t when t = typeof<Tag> ->
                    box (mtag.Materialize 0 reader) :?> 'a
                | _ -> failwithf "type %A not allowed for Get" typeof<'a>

        member _.InsertTagToApp app tag =
            exec "insert into App_Tag values (@AppId, @TagId)"
                {| AppId = app.Id; TagId = tag.Id |}

        member _.DeleteTagToApp app tag =
            exec "delete from App_Tag where AppId = @AppId and TagId = @TagId"
                {| AppId = app.Id; TagId = tag.Id |}
