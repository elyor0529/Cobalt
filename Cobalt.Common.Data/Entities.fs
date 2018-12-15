namespace Cobalt.Common.Data

open System

type Color = string

type Tag = {
    Id: int64
    Name: string
    ForegroundColor: Color
    BackgroundColor: Color
}

type App = {
    Id: int64
    Name: string
    Color: string
    Path: string
    Icon: Lazy<byte[]>
    Tags: IObservable<Tag>
}

type AppUsageStartReason = 
    | Switch
    | Start
    | Resume
    | MonitorOn

type AppUsageEndReason = 
    | Switch
    | Shutdown
    | Logoff
    | Suspend
    | MonitorOff

type AppUsageType = 
    | Focus
    | InView

type AppUsage = {
    Id: int64
    App: App
    Start: DateTimeOffset
    End: DateTimeOffset
    StartReason: AppUsageStartReason
    EndReason: AppUsageEndReason
    UsageType: AppUsageType
}

type ReminderAction = 
    | Warn
    | CustomWarn of string
    | Script of string

type Reminder = {
    Id: int64
    Offset: TimeSpan
    Action: ReminderAction
}

type OnceTimeRange = {
    Start: DateTimeOffset
    End: DateTimeOffset
}

type RepeatTimeRange =  
    | Daily
    | Weekly
    | Monthly

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
    Id: int64
    MaxDuration: TimeSpan
    Enabled: bool
    Reminders: IObservable<Reminder>
    Action: RunAction
    TimeRange: TimeRange
    Entity: Monitorable
}