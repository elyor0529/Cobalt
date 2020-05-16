namespace Cobalt.Common.Data.Repository

open System.Data
open Microsoft.Data.Sqlite
open Cobalt.Common.Data.Entities
open System
open System.IO
open Cobalt.Common.Data.Migrations.Meta
open System.Linq

module Helpers =
    let addParam key value (prms : SqliteParameterCollection) =
        prms.AddWithValue(key, value) |> ignore
        prms

    let toDateTime ticks = DateTime(ticks, DateTimeKind.Utc).ToLocalTime()
    let fromDateTime (dt:DateTime) = dt.ToUniversalTime().Ticks
    let toTimespan ticks = TimeSpan(ticks)
    let fromTimeSpan (ts:TimeSpan) = ts.Ticks

    let inline autoGenId< ^T when ^T: (member Id: int64)> (o: ^T) = 
        if (^T : (member Id: int64) (o)) <> 0L then
            addParam "Id" (^T : (member Id: int64) (o))
        else
            addParam "Id" (DBNull.Value)
    let appWith id = { Id = id; Name = null; Identification = Win32 null; Icon = null; Background = null; Tags = null  }
    let tagWith id = { Id = id; Name = null; Color = null; Apps = null }

open Helpers

[<AbstractClass>]
type Materializer<'a>(conn, schema: Schema) =

    let table = schema.tables.Item typeof<'a>.Name
    let fields = (table.fields.Select (fun x -> x.name)).ToArray()
    let fieldsCount = fields |> Array.length

    let fieldsStr = fields |> Array.map ((+) "@") |> String.concat ","
    let columnsStr = fields |> String.concat ","
    let columnsPrefixedStr = fields |> Array.map ((+) (table.name.ToLower() + ".")) |> String.concat ","

    let insertSql = sprintf "insert into %s(%s) values (%s); select last_insert_rowid()" table.name columnsStr fieldsStr

    let insertCmd =
        let cmd = new SqliteCommand(insertSql, conn);
        cmd.Prepare();
        cmd

    member _.Connection = conn
    member _.Schema = schema
    member _.Table = table
    member _.FieldsCount = fieldsCount
    member _.ColumnsStr = columnsStr
    member _.ColumnsPrefixedStr = columnsPrefixedStr
    member _.InsertCommand = insertCmd.Parameters.Clear(); insertCmd

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

    member _.DematerializeIdentification id = 
        match id with
        | Win32 path -> 
            addParam "Identification_Tag" 0  >>
            addParam "Identification_Text1" path
        | UWP praid -> 
            addParam "Identification_Tag" 1 >>
            addParam "Identification_Text1" praid
        | Java mainJar -> 
            addParam "Identification_Tag" 2 >>
            addParam "Identification_Text1" mainJar

    override x.Dematerialize obj prms = 
        use mem = new MemoryStream()
        obj.Icon.CopyTo(mem)
        let icon = mem.ToArray()
        prms
        |> autoGenId obj
        |> addParam "Name" obj.Name
        |> x.DematerializeIdentification obj.Identification
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
        { Id = id; Title = title; CmdLine = cmdLine; App = appWith appId }

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
            Start = toDateTime start;
            End = toDateTime ed;
            Session = { Id = sessid; Title = null; CmdLine = null; App = Unchecked.defaultof<App> }
        }

    override _.Dematerialize obj prms = 
        prms
        |> autoGenId obj
        |> addParam "Start" (fromDateTime obj.Start)
        |> addParam "End" (fromDateTime obj.End)
        |> addParam "SessionId" obj.Session.Id
        |> ignore

type SystemEventMaterializer(conn, sch) = 
    inherit Materializer<SystemEvent>(conn, sch)

    override _.Materialize offset reader =
        let id = reader.GetInt64(offset + 0)
        let timestamp = reader.GetInt64(offset + 1)
        let kind = reader.GetInt64(offset + 2)
        {
            Id = id;
            Timestamp = toDateTime timestamp;
            Kind = LanguagePrimitives.EnumOfValue kind;
        }

    override _.Dematerialize obj prms = 
        prms
        |> autoGenId obj
        |> addParam "Timestamp" (fromDateTime obj.Timestamp)
        |> addParam "Kind" (LanguagePrimitives.EnumToValue obj.Kind)
        |> ignore

type AlertMaterializer(conn, sch) = 
    inherit Materializer<Alert>(conn, sch)

    override _.Materialize offset reader =
        let id = reader.GetInt64(offset + 0)
        let target_appid = reader.GetValue(offset + 1)
        let target_tagid = reader.GetValue(offset + 2)
        let timerange_tag = reader.GetInt64(offset + 3)
        let timerange_integer1 = reader.GetInt64(offset + 4)
        let timerange_integer2 = reader.GetInt64(offset + 5)
        let timerange_integer3 = reader.GetValue(offset + 6)
        let usagelimit = reader.GetInt64(offset + 7)
        let exceededreaction_tag = reader.GetInt64(offset + 8)
        let exceededreaction_text1 = reader.GetValue(offset + 9)
        
        let target =
            if target_appid :? int64 then 
                appWith (target_appid :?> int64) |> App
            else
                tagWith (target_tagid :?> int64) |> Tag
        let timerange =
            match timerange_tag with
                | 0L -> Once (toDateTime timerange_integer1, toDateTime timerange_integer2)
                | 1L -> Repeated (LanguagePrimitives.EnumOfValue timerange_integer1, toTimespan timerange_integer2, toTimespan (timerange_integer3 :?> int64))
                | _ -> failwith "Unsupported"
        let exceededreaction =
            match exceededreaction_tag with
                | 0L -> Kill
                | 1L -> Message (exceededreaction_text1 :?> string)
                | _ -> failwith "Unsupported"
        {
            Id = id;
            Target = target;
            TimeRange = timerange;
            UsageLimit = (toTimespan usagelimit);
            ExceededReaction = exceededreaction;
        }

    override _.Dematerialize obj prms = 
        prms
        |> autoGenId obj
        |>
            match obj.Target with
            | App app ->
                addParam "Target_AppId" app.Id >>
                addParam "Target_TagId" DBNull.Value
            | Tag tag ->
                addParam "Target_AppId" DBNull.Value >>
                addParam "Target_TagId" tag.Id
        |>
            match obj.TimeRange with
            | Once (start, ed) ->
                addParam "TimeRange_Tag" 0 >>
                addParam "TimeRange_Integer1" (fromDateTime start) >>
                addParam "TimeRange_Integer2" (fromDateTime ed) >>
                addParam "TimeRange_Integer3" DBNull.Value
            | Repeated (typ, sod, eod) ->
                addParam "TimeRange_Tag" 1 >>
                addParam "TimeRange_Integer1" (LanguagePrimitives.EnumToValue typ) >>
                addParam "TimeRange_Integer2" (fromTimeSpan sod) >>
                addParam "TimeRange_Integer3" (fromTimeSpan eod)
        |> addParam "UsageLimit" (fromTimeSpan obj.UsageLimit)
        |> 
            match obj.ExceededReaction with
            | Kill -> 
                addParam "ExceededReaction_Tag" 0 >>
                addParam "ExceededReaction_Text1" DBNull.Value
            | Message msg -> 
                addParam "ExceededReaction_Tag" 1 >>
                addParam "ExceededReaction_Text1" msg
        |> ignore
