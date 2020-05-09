namespace Cobalt.Common.Data.Repository

open Cobalt.Common.Data.Entities
open Cobalt.Common.Data.Migrations
open Microsoft.Data.Sqlite
open Dapper

type IDbRepository = 
    abstract member Insert<'a> : 'a -> 'a
    abstract member Delete<'a> : 'a -> unit
    abstract member Get<'a> : int64 -> 'a

    abstract member InsertTagToApp : App -> Tag -> unit 
    abstract member DeleteTagToApp : App -> Tag -> unit 

type DbRepository (conn: SqliteConnection, mig: IMigrator) =
    let schema = mig.Migrate()

    let mapp = AppMaterializer(conn, schema)
    let mtag = TagMaterializer(conn, schema)
    let msess = SessionMaterializer(conn, schema)

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

    member x.IdReader<'a> id = singleReader (sprintf "select * from %s where Id = %d" (typeof<'a>.Name) id)

    member inline private _.Insert< ^a when ^a: (member Id:int64)> (o:'a) (m: Materializer<'a>) =
        let c = new SqliteCommand(m.InsertSql o, conn)
        m.Dematerialize o c.Parameters
        c.ExecuteScalar() :?> int64

    interface IDbRepository with
        member x.Insert obj = 
            match box obj with
                | :? App as o ->
                    let id = x.Insert o mapp
                    box { o with
                            Id = id;
                            Icon = new SqliteBlob(conn, "App", "Icon", id);
                            Tags = tagsFor id
                        } :?> 'a
                | :? Tag as o ->
                    let id = x.Insert o mtag
                    box { o with Id = id; Apps = appsFor id } :?> 'a
                | :? Session as o ->
                    let id = x.Insert o msess
                    box { o with Id = id; } :?> 'a
                | _ -> failwithf "type %A not allowed for Insert" (obj.GetType())

        member x.Delete obj =
            match box obj with
                | :? Alert as x -> ()
                | _ -> failwithf "type %A not allowed for Delete" (obj.GetType())

        member x.Get<'a> id = 
            match typeof<'a> with
                | t when t = typeof<App> ->
                    let reader = x.IdReader<App> id
                    let app = mapp.Materialize 0 reader
                    box { app with Tags = tagsFor app.Id } :?> 'a
                | t when t = typeof<Tag> ->
                    let reader = x.IdReader<Tag> id
                    let tag = mtag.Materialize 0 reader
                    box { tag with Apps = appsFor tag.Id } :?> 'a
                | t when t = typeof<Session> ->
                    let reader = x.IdReader<Session> id
                    let session = msess.Materialize 0 reader
                    box session :?> 'a
                | _ -> failwithf "type %A not allowed for Get" typeof<'a>

        member _.InsertTagToApp app tag =
            exec "insert into App_Tag values (@AppId, @TagId)"
                {| AppId = app.Id; TagId = tag.Id |}

        member _.DeleteTagToApp app tag =
            exec "delete from App_Tag where AppId = @AppId and TagId = @TagId"
                {| AppId = app.Id; TagId = tag.Id |}
