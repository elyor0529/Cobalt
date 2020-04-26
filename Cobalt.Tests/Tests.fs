module Tests

open Xunit
open FsUnit.Xunit

[<Fact>]
let ``1 plus 1 should equal 2`` () =
    1 + 1 |> should equal 2
