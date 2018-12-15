namespace Cobalt.Common.Data

open System.Data.SQLite
open Dapper

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

type AppInsert = {
    Name: string
    Color: Color
    Path: string
    Icon: byte[]
}

type AppUsageInsert = {
    AppId: int64
    Start: int64
    End: int64
    StartReason: int64
    EndReason: int64
    UsageType: int64
}

type IdDelete<'a> = {
    Id: 'a
}

type SQLiteRepository(conn: SQLiteConnection) = 

    let insert sql o = 
        conn.ExecuteScalar<'a>(sql+"; select last_insert_rowid()", o)
    let delete tbl id = 
        conn.Execute(sprintf "delete from %s where Id=@Id" tbl, {Id=id})
        |> ignore
    let enumToVal = LanguagePrimitives.EnumToValue 

    interface IDbRepository with
        member this.Insert(arg: App): unit = 
            arg.Id <- insert 
                "insert into App 
                (Name, Path, Color, Icon) values 
                (@Name, @Path, @Color, @Icon)"
                {
                    Name=arg.Name;
                    Color=arg.Color;
                    Path=arg.Path;
                    Icon=if arg.Icon <> null then arg.Icon.Value else null
                }
        member this.Insert(arg: Tag): unit = 
            arg.Id <- insert 
                "insert into Tag 
                (Name, ForegroundColor, BackgroundColor) values 
                (@Name, @ForegroundColor, @BackgroundColor)"
                arg
        member this.Insert(arg: AppUsage): unit = 
            arg.Id <- insert 
                "insert into AppUsage 
                (AppId, Start, End, StartReason, EndReason, UsageType) values 
                (@AppId, @Start, @End, @StartReason, @EndReason, @UsageType)"
                {
                    AppId = arg.App.Id;
                    Start = arg.Start.Ticks;
                    End = arg.End.Ticks;
                    StartReason = enumToVal arg.StartReason;
                    EndReason = enumToVal arg.EndReason;
                    UsageType = enumToVal arg.UsageType;
                }
        member this.Insert(arg: Reminder): unit = 
            raise (System.NotImplementedException())
        member this.Insert(arg: Alert): unit = 
            raise (System.NotImplementedException())

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

type GG(x) = 
    let g = SQLiteRepository(null)

    member this.X = 1