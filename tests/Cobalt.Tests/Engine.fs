module Engine

open Xunit
open FsUnit.Xunit
open Swensen.Unquote
open Cobalt.Tests.Util
open Cobalt.Engine.Watchers
open Vanara.PInvoke
open System.Threading

[<Fact>]
let ``adding numbers in FsUnit`` () = 
    1 + 1 |> should equal 2

[<Fact>]
let ``adding numbers in Unquote`` () = 
    test <@ 1 + 1 = 2 @>

[<Fact>]
let ``switching foreground`` () =
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

