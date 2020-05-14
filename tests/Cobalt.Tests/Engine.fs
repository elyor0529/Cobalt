module Engine

open Xunit
open Swensen.Unquote
open Cobalt.Tests.Util
open Cobalt.Engine.Watchers
open Vanara.PInvoke
open System.Threading

[<Fact>]
let ``switching foreground with two apps`` () =
    use proc1 = new Proc "winver.exe"
    use proc2 = new Proc "notepad.exe"

    let fgWatcher = new ForegroundWindowWatcher()
    let cts = new CancellationTokenSource()

    async {
        fgWatcher.Watch()
        let msgLoop = WatchLoop()
        let! _ = msgLoop.Run(cts.Token).AsTask() |> Async.AwaitTask
        fgWatcher.Dispose()
    } |> Async.Start

    let e = monitor fgWatcher (fun () -> Thread.Sleep(1000))
    test <@ e.isNothing @>

    let e = monitor fgWatcher (fun () -> delayed proc1.makeFg)
    test <@ e.isJustOneValue.IsSome @>

    let e = monitor fgWatcher (fun () -> delayed proc2.makeFg)
    test <@ e.isJustOneValue.IsSome @>

    let e = monitor fgWatcher (fun () -> delayed proc2.makeFg)
    test <@ e.isNothing @>
    cts.Cancel()

[<Fact>]
let ``switching foreground with more than two apps`` () =
    use proc1 = new Proc "winver.exe"
    use proc2 = new Proc "notepad.exe"
    use proc3 = new Proc @"C:\Program Files\Windows NT\Accessories\wordpad.exe"

    let fgWatcher = new ForegroundWindowWatcher()
    let cts = new CancellationTokenSource()

    async {
        fgWatcher.Watch()
        let msgLoop = WatchLoop()
        let! _ = msgLoop.Run(cts.Token).AsTask() |> Async.AwaitTask
        fgWatcher.Dispose()
    } |> Async.Start

    let e = monitor fgWatcher (fun () -> delayed proc1.makeFg)
    test <@ e.isJustOneValue.IsSome @>

    let e = monitor fgWatcher (fun () ->
        delayed proc2.makeFg;
        delayed proc3.makeFg;
        delayed proc1.makeFg;
        delayed proc3.makeFg)
    test <@ not e.completed && e.noExns && e.values.Length = 4 @>
    cts.Cancel()
