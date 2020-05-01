module Engine

open Xunit
open FsUnit.Xunit
open System.Threading
open Utils

[<Fact>]
let ``switching foreground`` () =
    use proc1 = new Proc "winver.exe"
    use proc2 = new Proc "notepad.exe"
    proc2.makeFg()
    Thread.Sleep(2000)
    proc1.makeFg()
    Thread.Sleep(2000)
    proc2.makeFg()
    Thread.Sleep(2000)
    proc1.makeFg()

