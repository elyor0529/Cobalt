namespace Cobalt.Common.Data.Entities

open System
open System.IO

type AppIdentification =
    | Win32 of Path: string
    | UWP of PRAID: string
    | Java of MainJar: string

[<CLIMutable>]
type App = {
    Id: int64;
    Name: string;
    Identification: AppIdentification;
    Background: string;
    Icon: Stream
    Tags: Lazy<Tag seq>;
}
and [<CLIMutable>] Tag = {
    Id: int64;
    Name: string;
    Color: string;
    Apps: Lazy<App seq>;
}

[<CLIMutable>]
type Session = {
    Id: int64;
    Title: string;
    CmdLine: string;
    App: App;
}

[<CLIMutable>]
type Usage = {
    Id: int64;
    Start: DateTime;
    End: DateTime;
    Session: Session;
}

type SystemEventKind = Logon = 0L | Logoff = 1L | Active = 2L | Idle = 3L

[<CLIMutable>]
type SystemEvent = {
    Id: int64;
    Timestamp: DateTime;
    Kind: SystemEventKind;
}

type Target = App of App: App | Tag of App: Tag

type TimeRange =
    | Once of Start: DateTime * End: DateTime
    | Repeated of Type: RepeatType * StartOfDay: TimeSpan * EndOfDay: TimeSpan
and RepeatType = Daily = 0L | Weekly = 1L | Monthly = 2L

type Reaction = 
    | Kill
    | Message of Message: string

[<CLIMutable>]
type Alert = {
    Id: int64;
    Target: Target;
    TimeRange: TimeRange;
    UsageLimit: TimeSpan;
    ExceededReaction: Reaction;
}
