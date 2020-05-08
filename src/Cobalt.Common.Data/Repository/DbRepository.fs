namespace Cobalt.Common.Data.Repository

open Cobalt.Common.Data.Entities
open Cobalt.Common.Data.Migrations
open Microsoft.Data.Sqlite
open Microsoft.FSharp.Reflection

type IDbRepository = 
    abstract member Insert<'a> : 'a -> unit
    abstract member Delete<'a> : 'a -> unit
    abstract member Get<'a> : int64 -> 'a

type DbRepository (conn: SqliteConnection, mig: IMigrator) =
    let schema = mig.Migrate()

    let parameter name (value:obj) = new SqliteParameter(name, value)


    interface IDbRepository with
        member x.Insert obj = 
            match box obj with
                | :? Alert as x -> ()
                | _ -> failwithf "type %A not allowed for Insert" (obj.GetType())

        member x.Delete obj =
            match box obj with
                | :? Alert as x -> ()
                | _ -> failwithf "type %A not allowed for Delete" (obj.GetType())
        member x.Get<'a> id = raise<'a> (exn ())
