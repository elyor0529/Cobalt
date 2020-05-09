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

    let table = schema.tables.Item typeof<'a>.Name
    let fields = (table.fields.Select (fun x -> x.name)).ToArray()
    let fieldsCount = fields |> Array.length
    let fieldsWithoutId = (fields |> Seq.except ["Id"]).ToArray()

    let fieldsStr = fields |> Array.map ((+) "@") |> String.concat ","
    let fieldsWithoutIdStr = fieldsWithoutId |> Array.map ((+) "@") |> String.concat ","
    let columnsStr = fields |> String.concat ","
    let columnsPrefixedStr = fields |> Array.map ((+) (table.name.ToLower() + ".")) |> String.concat ","
    let columnsWithoutIdStr = fieldsWithoutId |> String.concat ","

    let insertSql = sprintf "insert into %s(%s) values (%s); select last_insert_rowid()" table.name columnsStr fieldsStr
    let insertWithoutIdSql = sprintf "insert into %s(%s) values (%s); select last_insert_rowid()" table.name columnsWithoutIdStr fieldsWithoutIdStr

    let insertCmd =
        let cmd = new SqliteCommand(insertSql, conn);
        cmd.Prepare();
        cmd
    let insertWithoutIdCmd =
        let cmd = new SqliteCommand(insertWithoutIdSql, conn);
        cmd.Prepare();
        cmd

    member _.Connection = conn
    member _.Schema = schema
    member _.Table = table
    member _.FieldsCount = fieldsCount
    member _.ColumnsStr = columnsStr
    member _.ColumnsPrefixedStr = columnsPrefixedStr
    member _.ColumnsWithoutIdStr = columnsWithoutIdStr
    member _.InsertWithoutIdCmd = insertWithoutIdCmd
    member _.InsertCmd = insertCmd

    member inline x.InsertCommand< ^T when ^T: (member Id: int64)> (o: ^T) =
        if (^T : (member Id: int64) (o)) = 0L
        then x.InsertWithoutIdCmd
        else x.InsertCmd

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
        { Id = id; Title = title; CmdLine = cmdLine; App = { Id = appId; Name = null; Identification = Win32 null; Icon = null; Background = null; Tags = null } }

    override _.Dematerialize obj prms = 
        prms
        |> autoGenId obj
        |> addParam "Title" obj.Title
        |> addParam "CmdLine" obj.CmdLine
        |> addParam "AppId" obj.App.Id
        |> ignore

type UsageMaterializer(conn, sch) =
    inherit Materializer<Usage>(conn, sch)

    override _.Materialize offset reader =
        let id = reader.GetInt64(offset + 0)
        let start = reader.GetInt64(offset + 1)
        let ed = reader.GetInt64(offset + 2)
        let sessid = reader.GetInt64(offset + 3)
        {
            Id = id;
            Start = new DateTime(start, DateTimeKind.Utc);
            End = new DateTime(ed, DateTimeKind.Utc);
            Session = { Id = sessid; Title = null; CmdLine = null; App = Unchecked.defaultof<App> }
        }

    override _.Dematerialize obj prms = 
        prms
        |> autoGenId obj
        |> addParam "Start" (obj.Start.ToUniversalTime().Ticks)
        |> addParam "End" (obj.End.ToUniversalTime().Ticks)
        |> addParam "SessionId" obj.Session.Id
        |> ignore
