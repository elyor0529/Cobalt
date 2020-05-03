namespace Cobalt.Common.Data.Entities

open System


type AppIdentification =
    | Win32 of Win32
    | UWP of UWP
    | Java of Java
and Win32 = { Path: string; }
    with static member Id path = Win32 { Path = path }
and UWP = { PRAID: string }
    with static member Id praid = UWP { PRAID = praid }
and Java = { MainJar: string }
    with static member Id mainJar = Java { MainJar = mainJar }

[<CLIMutable>]
type App = {
    Id: int64;
    Name: string;
    Identification: AppIdentification;
    Background: string;
    Icon: Lazy<byte[]>;
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

type SystemEventKind = Logon | Logoff | Active | Idle

[<CLIMutable>]
type SystemEvent = {
    Id: int64;
    Timestamp: DateTime;
    Kind: SystemEventKind;
}

type Target = App of App | Tag of Tag

type TimeRange =
    | Once of Once
    | Repeated of Repeated
and Once = {
    Start: DateTime;
    End:DateTime }
    with static member TimeRange s e = Once { Start = s; End = e }
and Repeated = {
    StartOfDay: TimeSpan;
    EndOfDay: TimeSpan;
    Type: RepeatType }
    with static member TimeRange s e t = Repeated { StartOfDay = s; EndOfDay = e; Type = t }
and RepeatType = Daily | Weekly | Monthly

type Reaction = 
    | Kill
    | Message of string

[<CLIMutable>]
type Alert = {
    Id: int64;
    Target: Target;
    TimeRange: TimeRange;
    UsageLimit: TimeSpan;
    ExceededReaction: Reaction;
}
