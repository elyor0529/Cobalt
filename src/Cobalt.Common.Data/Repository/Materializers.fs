namespace Cobalt.Common.Data.Repository

open System.Data
open Microsoft.Data.Sqlite
open Cobalt.Common.Data.Entities
open System.Linq

module Helpers =
    let addParam key value (prms : SqliteParameterCollection) =
        prms.AddWithValue(key, value) |> ignore
        prms
    let inline autoGenId< ^T when ^T: (member Id: int64)> (o: ^T) = 
        if (^T : (member Id: int64) (o)) <> 0L then
            addParam "Id" (^T : (member Id: int64) (o))
        else id

open Helpers
open System
open System.IO
open Cobalt.Common.Data.Migrations.Meta

[<AbstractClass>]
type Materializer<'a>(conn, schema: Schema) =

    member inline x.InsertSql< ^T when ^T: (member Id: int64)> (o: ^T) =
        let (cols, flds) = if (^T : (member Id: int64) (o)) = 0L then (x.ColumnsWithoutIdStr, x.FieldsWithoutIdStr) else (x.ColumnsStr, x.FieldsStr)
        sprintf "insert into %s(%s) values (%s); select last_insert_rowid()" x.Table.name cols flds

    member _.Connection = conn
    member _.Schema = schema
    member _.Table: Table = schema.tables.Item typeof<'a>.Name
    member x.Fields = (x.Table.fields.Select (fun x -> x.name)).ToArray()
    member x.FieldsCount = x.Fields |> Array.length
    member x.FieldsWithoutId = (x.Fields |> Seq.except ["Id"]).ToArray()

    member x.FieldsStr = x.Fields |> Array.map ((+) "@") |> String.concat ","
    member x.FieldsWithoutIdStr = x.FieldsWithoutId |> Array.map ((+) "@") |> String.concat ","
    member x.ColumnsStr = x.Fields |> String.concat ","
    member x.ColumnsPrefixedStr = x.Fields |> Array.map ((+) (x.Table.name.ToLower() + ".")) |> String.concat ","
    member x.ColumnsWithoutIdStr = x.FieldsWithoutId |> String.concat ","


    abstract member Materialize: int -> IDataReader -> 'a
    abstract member Dematerialize: 'a -> SqliteParameterCollection -> unit

type AppMaterializer(conn, sch) =
    inherit Materializer<App>(conn, sch)

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
        let icon = (reader :?> SqliteDataReader).GetStream(offset + 5)
        { Id = id; Name = name; Identification = ident; Background = bg; Icon = icon; Tags = null; }

    override _.Dematerialize obj prms = 
        use mem = new MemoryStream()
        obj.Icon.CopyTo(mem)
        let icon = mem.ToArray()
        prms
        |> autoGenId obj
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

type TagMaterializer(conn, sch) =
    inherit Materializer<Tag>(conn, sch)

    override _.Materialize offset reader =
        let id = reader.GetInt64(offset + 0)
        let name = reader.GetString(offset + 1)
        let color = reader.GetString(offset + 2)
        { Id = id; Name = name; Color = color; Apps = null }

    override _.Dematerialize obj prms = 
        prms
        |> autoGenId obj
        |> addParam "Name" obj.Name
        |> addParam "Color" obj.Color
        |> ignore

type SessionMaterializer(conn, sch) =
    inherit Materializer<Session>(conn, sch)

    override _.Materialize offset reader =
        let id = reader.GetInt64(offset + 0)
        let title = reader.GetString(offset + 1)
        let cmdLine = reader.GetString(offset + 2)
        let appId = reader.GetInt64(offset + 3)
        { Id = id; Title = title; CmdLine = cmdLine; App = { Id = appId; Name = ""; Identification = Win32 ""; Icon = null; Background = null; Tags = null } }

    override _.Dematerialize obj prms = 
        prms
        |> autoGenId obj
        |> addParam "Title" obj.Title
        |> addParam "CmdLine" obj.CmdLine
        |> addParam "AppId" obj.App.Id
        |> ignore
