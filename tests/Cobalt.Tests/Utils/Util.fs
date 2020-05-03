module Cobalt.Tests.Util

open System
open System.Threading
open System.Diagnostics
open Vanara.PInvoke

let padDelay (num:int) fn = 
    Thread.Sleep(num)
    fn ()
    Thread.Sleep(num)

let delayed fn = padDelay 2000 fn

type Proc (fName: string) =
    let VK_MENU = 0x12uy
    let state = 0x80uy
    let proc = Process.Start(ProcessStartInfo(FileName = fName, WindowStyle = ProcessWindowStyle.Normal))
    let mainWindowHWND () = HWND proc.MainWindowHandle

    let isMainWindowVisible () =
        User32.ShowWindow(mainWindowHWND(), ShowWindowCommand.SW_NORMAL)

    do
        proc.WaitForInputIdle() |> ignore
        while String.IsNullOrEmpty proc.MainWindowTitle do
            Thread.Sleep 100
            proc.Refresh()


    member _.makeFg () =
        let hwnd = mainWindowHWND
        let keyState = Array.create 256 0uy

        if User32.GetKeyboardState(keyState) && ((keyState.[VK_MENU |> int] &&& state) = 0uy) then
            User32.keybd_event(VK_MENU, 0uy, User32.KEYEVENTF.KEYEVENTF_EXTENDEDKEY, unativeint 0)

        User32.SetForegroundWindow(hwnd()) |> ignore

        if User32.GetKeyboardState(keyState) && ((keyState.[VK_MENU |> int] &&& state) = 0uy) then
            User32.keybd_event(VK_MENU, 0uy, User32.KEYEVENTF.KEYEVENTF_EXTENDEDKEY ||| User32.KEYEVENTF.KEYEVENTF_KEYUP, unativeint 0)

    interface System.IDisposable with 
        member _.Dispose() = 
            proc.Kill()

type MonitorResults<'a> () = 
    member val values = List.empty with get, set
    member val exns = List.empty with get, set
    member val completed = false with get, set

    member x.noExns = List.length x.exns = 0
    member x.noValues = List.length x.values = 0

    member x.isJustOneValue =
        if x.noExns && not x.completed && List.length x.values <> 1 then
            None
        else Some(List.exactlyOne x.values)

    member x.isNothing = x.noExns && not x.completed && List.length x.values = 0

    interface IObserver<'a> with
        member x.OnNext v =
            x.values <- v :: x.values
        member x.OnError e =
            x.exns <- e :: x.exns
        member x.OnCompleted () = 
            x.completed <- true

let monitor<'a> (obs:IObservable<'a>) fn =
    let results = MonitorResults()
    use __ = obs.Subscribe(results)
    fn ()
    results
