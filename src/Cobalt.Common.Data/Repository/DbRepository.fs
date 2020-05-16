namespace Cobalt.Common.Data.Repository

open Cobalt.Common.Data.Entities
open Cobalt.Common.Data.Migrations
open Microsoft.Data.Sqlite
open Dapper
open Microsoft.Extensions.Logging

type IDbRepository = 
    inherit System.IDisposable
    abstract member Insert<'a> : 'a -> 'a
    abstract member Delete<'a> : 'a -> unit
    abstract member Get<'a> : int64 -> 'a

    abstract member FindAppByIdentification: AppIdentification -> App voption

    abstract member InsertTagToApp : App -> Tag -> unit 
    abstract member DeleteTagToApp : App -> Tag -> unit 

type DbRepository (conn: SqliteConnection, mig: IMigrator, logger: ILogger<DbRepository>) =
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

    member _.IdReader<'a> (mat: Materializer<'a>) id =
        use reader = singleReader (sprintf "select * from %s where Id = %d" (typeof<'a>.Name) id)
        mat.Materialize 0 reader

    member inline private _.Insert< ^a when ^a: ( member Id: int64)> (o:'a) (m: Materializer<'a>) =
        let c = m.InsertCommand
        m.Dematerialize o c.Parameters
        c.ExecuteScalar() :?> int64

    interface IDbRepository with
        member _.Dispose() = 
            conn.Dispose()
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
            logger.LogTrace("Inserted {obj}", ret);
            ret :?> 'a

        member x.Delete obj =
            match box obj with
                | :? Alert as x -> ()
                | _ -> failwithf "type %A not allowed for Delete" (obj.GetType())
            logger.LogTrace("Deleted {obj}", obj);

        member x.Get<'a> id = 
            let ret =
                match typeof<'a> with
                | t when t = typeof<App> ->
                    let app = x.IdReader mapp id
                    box { app with Tags = tagsFor app.Id } :?> 'a
                | t when t = typeof<Tag> ->
                    let tag = x.IdReader mtag id
                    box { tag with Apps = appsFor tag.Id } :?> 'a
                | t when t = typeof<Session> ->
                    let session = x.IdReader msess id
                    box session :?> 'a
                | t when t = typeof<Usage> ->
                    let usage = x.IdReader musage id
                    box usage :?> 'a
                | t when t = typeof<SystemEvent> ->
                    let se = x.IdReader mse id
                    box se :?> 'a
                | t when t = typeof<Alert> ->
                    let alt = x.IdReader malt id
                    box alt :?> 'a
                | _ -> failwithf "type %A not allowed for Get" typeof<'a>
            logger.LogTrace("Got <{id}> {obj}", id, ret);
            ret

        member _.FindAppByIdentification appId =
            let cmd = cmd "select * from App where Identification_Tag=@Identification_Tag and Identification_Text1=@Identification_Text1"
            mapp.DematerializeIdentification appId cmd.Parameters |> ignore
            let reader = cmd.ExecuteReader()
            let res = reader.Read()
            if res then
                ValueSome (mapp.Materialize 0 reader)
            else ValueNone

        member _.InsertTagToApp app tag =
            exec "insert into App_Tag values (@AppId, @TagId)"
                {| AppId = app.Id; TagId = tag.Id |}

        member _.DeleteTagToApp app tag =
            exec "delete from App_Tag where AppId = @AppId and TagId = @TagId"
                {| AppId = app.Id; TagId = tag.Id |}
