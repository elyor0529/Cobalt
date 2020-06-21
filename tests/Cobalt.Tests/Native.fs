module Native

open Xunit
open Swensen.Unquote
open Cobalt.Common.Native
open Microsoft.FSharp.NativeInterop

[<Fact>]
let ``range fails when end < start`` () =
    let mutable count_n = 0
    let mutable count_e = 0
    let mutable count_c = 0
    let mutable erV = None
    let next = Ffi.OnNext(fun i -> count_n <- count_n + 1)
    let err = Ffi.OnError(fun e -> count_e <- count_e + 1; erV <- Some(NativeException.From(e)))
    let complete = Ffi.OnComplete(fun e -> count_c <- count_c + 1)
    let sub = Ffi.Subscription(next, err, complete)
    Functions.range(32u, 1u, &sub)
    test <@ count_n = 0 @>
    test <@ count_e = 1 @>
    test <@ count_c = 1 @>
    test <@ erV.IsSome && erV.Value.Message = "end cannot be before start" @>


[<Fact>]
let ``range gives values`` () =
    let mutable arr = []
    let next = Ffi.OnNext(fun i -> arr <- NativePtr.read (NativePtr.ofVoidPtr i) :: arr)
    let err = Ffi.OnError(fun e -> ())
    let complete = Ffi.OnComplete(fun e -> ())
    let sub = Ffi.Subscription(next, err, complete)
    Functions.range(1u, 5u, &sub)
    test <@ arr = [4;3;2;1] @>
