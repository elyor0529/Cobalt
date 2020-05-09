namespace Cobalt.Common.Data.Repository

open System.Data
open Microsoft.Data.Sqlite
open Cobalt.Common.Data.Entities

module Helpers =
    let addParam key value (prms : SqliteParameterCollection) =
        prms.AddWithValue(key, value) |> ignore
        prms

open Helpers
open System
open System.IO

[<AbstractClass>]
type Materializer<'a>(conn) =
    member _.Connection = conn
    abstract member Materialize: int -> SqliteDataReader -> 'a
    abstract member Dematerialize: 'a -> SqliteParameterCollection -> unit

type AppMaterializer(conn) =
    inherit Materializer<App>(conn)

    override _.Materialize offset reader =
        let id = reader.GetInt64(offset + 0)
        let name = reader.GetString(offset + 1)
        let ident_tag = reader.GetInt64(offset + 2)
        let ident_text1 = reader.GetString(offset + 3)
        let ident = match ident_tag with
                    | 0L -> Win32 ident_text1
                    | 1L -> UWP ident_text1
                    | 2L -> Java ident_text1
                    | _ -> failwith "unsupported tag"
        let bg = reader.GetString(offset + 4)
        let icon = reader.GetStream(offset + 5)
        { Id = id; Name = name; Identification = ident; Background = bg; Icon = icon; Tags = null; }

    override _.Dematerialize obj prms = 
        use mem = new MemoryStream()
        obj.Icon.CopyTo(mem)
        let icon = mem.ToArray()
        prms
        |> addParam "Id" obj.Id
        |> addParam "Name" obj.Name
        |>
            match obj.Identification with
            | Win32 path -> 
                addParam "Identification_Tag" 0  >>
                addParam "Identification_Text1" path
            | UWP praid -> 
                addParam "Identification_Tag" 1 >>
                addParam "Identification_Text1" praid
            | Java mainJar -> 
                addParam "Identification_Tag" 2 >>
                addParam "Identification_Text1" mainJar
        |> addParam "Background" obj.Background
        |> addParam "Icon" icon
        |> ignore

type TagMaterializer(conn) =
    inherit Materializer<Tag>(conn)

    override _.Materialize offset reader =
        let id = reader.GetInt64(offset + 0)
        let name = reader.GetString(offset + 1)
        let color = reader.GetString(offset + 2)
        { Id = id; Name = name; Color = color; Apps = null }

    override _.Dematerialize obj prms = 
        prms
        |> addParam "Id" obj.Id
        |> addParam "Name" obj.Name
        |> addParam "Color" obj.Color
        |> ignore

