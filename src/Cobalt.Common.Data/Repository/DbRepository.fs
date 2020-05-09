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
    let musage = UsageMaterializer(conn, schema)
    let mse = SystemEventMaterializer(conn, schema)
    let malt = AlertMaterializer(conn, schema)

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

    member inline private _.Insert< ^a when ^a: ( member Id: int64)> (o:'a) (m: Materializer<'a>) =
        let c = m.InsertCommand
        m.Dematerialize o c.Parameters
        c.ExecuteScalar() :?> int64

    interface IDbRepository with
        member x.Insert obj = 
            let ret =
                match box obj with
                | :? App as o ->
                    let id = x.Insert o mapp
                    box { o with
                            Id = id;
                            Icon = new SqliteBlob(conn, "App", "Icon", id);
                            Tags = tagsFor id
                        }
                | :? Tag as o ->
                    let id = x.Insert o mtag
                    box { o with Id = id; Apps = appsFor id }
                | :? Session as o ->
                    let id = x.Insert o msess
                    box { o with Id = id; }
                | :? Usage as o ->
                    let id = x.Insert o musage
                    box { o with Id = id; }
                | :? SystemEvent as o ->
                    let id = x.Insert o mse
                    box { o with Id = id; }
                | :? Alert as o ->
                    let id = x.Insert o malt
                    box { o with Id = id; }
                | _ -> failwithf "type %A not allowed for Insert" (obj.GetType())
            ret :?> 'a

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
                | t when t = typeof<Usage> ->
                    let reader = x.IdReader<Usage> id
                    let usage = musage.Materialize 0 reader
                    box usage :?> 'a
                | t when t = typeof<SystemEvent> ->
                    let reader = x.IdReader<SystemEvent> id
                    let se = mse.Materialize 0 reader
                    box se :?> 'a
                | t when t = typeof<Alert> ->
                    let reader = x.IdReader<Alert> id
                    let alt = malt.Materialize 0 reader
                    box alt :?> 'a
                | _ -> failwithf "type %A not allowed for Get" typeof<'a>

        member _.InsertTagToApp app tag =
            exec "insert into App_Tag values (@AppId, @TagId)"
                {| AppId = app.Id; TagId = tag.Id |}

        member _.DeleteTagToApp app tag =
            exec "delete from App_Tag where AppId = @AppId and TagId = @TagId"
                {| AppId = app.Id; TagId = tag.Id |}
