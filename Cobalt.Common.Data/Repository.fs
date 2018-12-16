namespace Cobalt.Common.Data

open System.Data.SQLite
open Dapper
open System.Text
open Common.Common.Data.Migrations

type IDbRepository = 
    abstract member Insert: App -> unit
    abstract member Insert: Tag -> unit
    abstract member Insert: AppUsage -> unit
    abstract member Insert: Reminder -> unit
    abstract member Insert: Alert -> unit

    abstract member Update: App -> unit
    abstract member Update: Tag -> unit
    abstract member Update: AppUsage -> unit
    abstract member Update: Reminder -> unit
    abstract member Update: Alert -> unit

    abstract member Delete: App -> unit
    abstract member Delete: Tag -> unit
    abstract member Delete: AppUsage -> unit
    abstract member Delete: Reminder -> unit
    abstract member Delete: Alert -> unit

[<CLIMutable>]
type AppObj = {
    Id: int64
    Name: string
    Color: Color
    Path: string
    Icon: byte[]
}

[<CLIMutable>]
type AppUsageObj = {
    Id: int64
    AppId: int64
    Start: int64
    End: int64
    StartReason: int64
    EndReason: int64
    UsageType: int64
}

[<CLIMutable>]
type ReminderObj = {
    Id: int64
    Offset: int64
    ActionType: int64
    ActionParam: string
}

[<CLIMutable>]
type AlertObj = {
    Id: int64
    MaxDuration: int64
    Enabled: bool
    ActionType: int64
    ActionParam: string
    TimeRangeType: int64
    TimeRangeParam1: int64
    TimeRangeParam2: int64
    EntityType: int64
    Entity: int64
}

type IdObj<'a> = {
    Id: 'a
}

type SQLiteRepository(conn: SQLiteConnection, mig: SQLiteMigrator) = 

    do (mig :> IDbMigrator).Migrate()

    let insert (tbl:string) (fields:string[]) o = 
        let sql =
            StringBuilder()
                .Append("insert into ")
                .Append(tbl)
                .Append(" (")
                .Append(String.concat "," fields)
                .Append(") values (")
                .Append(String.concat "," (fields |> Seq.map (fun x -> "@"+x)))
                .Append("); select last_insert_rowid();")
        conn.ExecuteScalar<'a>(sql.ToString(), o)
    let delete tbl id = 
        conn.Execute(sprintf "delete from %s where Id=@Id" tbl, {Id=id})
        |> ignore
    let enumToVal = LanguagePrimitives.EnumToValue 

    let toAppObj (app:App) =
        {
            Id=app.Id;
            Name=app.Name;
            Color=app.Color;
            Path=app.Path;
            Icon=
            match app.Icon with
                | null -> null
                | a -> a.Value
        }
    let toAppUsageObj (au:AppUsage) = 
        {
            Id=au.Id;
            AppId = au.App.Id;
            Start = au.Start.Ticks;
            End = au.End.Ticks;
            StartReason = enumToVal au.StartReason;
            EndReason = enumToVal au.EndReason;
            UsageType = enumToVal au.UsageType;
        }
    let toReminderObj (re: Reminder) = 
        let (actionType, actionParam) =
            match re.Action with
            | ReminderAction.Warn -> (0L, null)
            | ReminderAction.CustomWarn(s) -> (1L, s)
            | ReminderAction.Script(s) -> (2L, s)
        {
            Id=re.Id;
            Offset=re.Offset.Ticks;
            ActionType=actionType;
            ActionParam=actionParam;
        }
    let toAlertObj (a: Alert) = 
        let (actionType, actionParam) =
            match a.Action with
            | RunAction.Message -> (0L, null)
            | RunAction.CustomMessage(s) -> (1L, s)
            | RunAction.Script(s) -> (2L, s)
            | RunAction.Kill -> (3L, null)
        let (timeRangeType, timeRangeParam1, timeRangeParam2) = 
            match a.TimeRange with
            | TimeRange.Once(once) -> (0L, once.Start.Ticks, once.End.Ticks)
            | TimeRange.Repeat(re) -> (1L, enumToVal re, 0L)
        let (entityType, entity) =
            match a.Entity with
            | Monitorable.App(a) -> (0L, a.Id)
            | Monitorable.Tag(t) -> (1L, t.Id)
        {
            Id=a.Id;
            MaxDuration=a.MaxDuration.Ticks;
            Enabled=a.Enabled;
            ActionType=actionType;
            ActionParam=actionParam;
            TimeRangeType=timeRangeType;
            TimeRangeParam1=timeRangeParam1;
            TimeRangeParam2=timeRangeParam2;
            EntityType=entityType;
            Entity=entity;
        }

    interface IDbRepository with
        member this.Insert(arg: App): unit = 
            arg.Id <- insert "App" 
                [|"Name"; "Path"; "Color"; "Icon"|]
                (toAppObj arg)
        member this.Insert(arg: Tag): unit = 
            arg.Id <- insert "Tag"
                [|"Name"; "ForegroundColor"; "BackgroundColor"|]
                arg
        member this.Insert(arg: AppUsage): unit = 
            arg.Id <- insert "AppUsage"
                [|"AppId"; "Start"; "End"; "StartReason"; "EndReason"; "UsageType"|]
                (toAppUsageObj arg)
        member this.Insert(arg: Reminder): unit = 
            arg.Id <- insert "Reminder" 
                [|"Offset"; "ActionType"; "ActionParam"|]
                (toReminderObj arg)
        member this.Insert(arg: Alert): unit = 
            arg.Id <- insert "Alert" 
                [|"MaxDuration"; "Enabled"; "ActionType"; "ActionParam"; "TimeRangeType";
                "TimeRangeParam1"; "TimeRangeParam2"; "EntityType"; "Entity"|]
                (toAlertObj arg)

        member this.Delete(arg: App): unit = 
            delete "App" arg.Id
        member this.Delete(arg: Tag): unit = 
            delete "Tag" arg.Id
        member this.Delete(arg: AppUsage): unit = 
            delete "AppUsage" arg.Id
        member this.Delete(arg: Reminder): unit = 
            delete "Reminder" arg.Id
        member this.Delete(arg: Alert): unit = 
            //cascade delete!!
            delete "Alert" arg.Id

        member this.Update(arg: App): unit = 
            raise (System.NotImplementedException())
        member this.Update(arg: Tag): unit = 
            raise (System.NotImplementedException())
        member this.Update(arg: AppUsage): unit = 
            raise (System.NotImplementedException())
        member this.Update(arg: Reminder): unit = 
            raise (System.NotImplementedException())
        member this.Update(arg: Alert): unit = 
            raise (System.NotImplementedException())
