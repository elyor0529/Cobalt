namespace Cobalt.Common.Data

open System

type Color = string

type Tag = {
    mutable Id: int64
    Name: string
    ForegroundColor: Color
    BackgroundColor: Color
}

type App = {
    mutable Id: int64
    Name: string
    Color: string
    Path: string
    Icon: Lazy<byte[]>
    Tags: IObservable<Tag>
}

type AppUsageStartReason = 
    | Switch=0L
    | Start=1L
    | Resume=2L
    | MonitorOn=3L

type AppUsageEndReason = 
    | Switch=0L
    | Shutdown=1L
    | Logoff=2L
    | Suspend=3L
    | MonitorOff=4L

type AppUsageType = 
    | Focus=0L
    | InView=1L

type AppUsage = {
    mutable Id: int64
    App: App
    Start: DateTime
    End: DateTime
    StartReason: AppUsageStartReason
    EndReason: AppUsageEndReason
    UsageType: AppUsageType
}

type ReminderAction = 
    | Warn
    | CustomWarn of string
    | Script of string

type Reminder = {
    mutable Id: int64
    Offset: TimeSpan
    Action: ReminderAction
}

type OnceTimeRange = {
    Start: DateTime
    End: DateTime
}

type RepeatTimeRange =  
    | Daily=0L
    | Weekly=1L
    | Monthly=2L

type TimeRange = 
    | Once of OnceTimeRange
    | Repeat of RepeatTimeRange

type RunAction = 
    | Message
    | CustomMessage of string
    | Script of string
    | Kill

type Monitorable = 
    | App of App
    | Tag of Tag

type Alert = {
    mutable Id: int64
    MaxDuration: TimeSpan
    Enabled: bool
    Reminders: IObservable<Reminder>
    Action: RunAction
    TimeRange: TimeRange
    Entity: Monitorable
}