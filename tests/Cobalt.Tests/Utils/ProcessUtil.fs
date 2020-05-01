module Utils

open System.Diagnostics
open Vanara.PInvoke
open System

type Proc (fName: string) =
    let proc = Process.Start(ProcessStartInfo(FileName = fName, WindowStyle = ProcessWindowStyle.Normal))
    do
        proc.WaitForInputIdle() |> ignore

    member _.makeFg () =
        let hwnd = HWND proc.MainWindowHandle
        let keyState = Array.create 256 0uy
        let VK_MENU = 0x12uy
        let state = 0x80uy

        if User32.GetKeyboardState(keyState) && ((keyState.[VK_MENU |> int] &&& state) = 0uy) then
            User32.keybd_event(VK_MENU, 0uy, User32.KEYEVENTF.KEYEVENTF_EXTENDEDKEY, unativeint 0)

        User32.SetForegroundWindow(hwnd) |> ignore

        if User32.GetKeyboardState(keyState) && ((keyState.[VK_MENU |> int] &&& state) = 0uy) then
            User32.keybd_event(VK_MENU, 0uy, User32.KEYEVENTF.KEYEVENTF_EXTENDEDKEY ||| User32.KEYEVENTF.KEYEVENTF_KEYUP, unativeint 0)

    interface System.IDisposable with 
        member _.Dispose() = 
            proc.Kill()
    
