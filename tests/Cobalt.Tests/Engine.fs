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

    use fgWatcher = new ForegroundWindowWatcher()

    async {
        fgWatcher.Watch()
        let msgLoop = MessageLoop()
        msgLoop.Run()
    } |> Async.Start

    let e = monitor fgWatcher (fun () -> Thread.Sleep(1000))
    test <@ e.isNothing @>

    let e = monitor fgWatcher (fun () -> padDelay 1000 proc1.makeFg)
    test <@ e.isJustOneValue.IsSome @>

    let e = monitor fgWatcher (fun () -> padDelay 1000 proc2.makeFg)
    test <@ e.isJustOneValue.IsSome @>

    let e = monitor fgWatcher (fun () -> padDelay 1000 proc2.makeFg)
    test <@ e.isNothing @>

[<Fact>]
let ``switching foreground with more than two apps`` () =
    use proc1 = new Proc "winver.exe"
    use proc2 = new Proc "notepad.exe"
    use proc3 = new Proc @"C:\Program Files\Windows NT\Accessories\wordpad.exe"

    use fgWatcher = new ForegroundWindowWatcher()

    async {
        fgWatcher.Watch()
        let msgLoop = MessageLoop()
        msgLoop.Run()
    } |> Async.Start

    let e = monitor fgWatcher (fun () -> padDelay 1000 proc1.makeFg)
    test <@ e.isJustOneValue.IsSome @>

    let e = monitor fgWatcher (fun () ->
        padDelay 1000 proc2.makeFg;
        padDelay 1000 proc3.makeFg;
        padDelay 1000 proc1.makeFg;
        padDelay 1000 proc3.makeFg)
    test <@ not e.completed && e.noExns && e.values.Length = 4 @>
