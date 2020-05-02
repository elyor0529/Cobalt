module General

open Xunit
open FsUnit.Xunit
open Swensen.Unquote

[<Fact>]
let ``adding numbers in FsUnit`` () = 
    1 + 1 |> should equal 2

[<Fact>]
let ``adding numbers in Unquote`` () = 
    test <@ 1 + 1 = 2 @>

[<CLIMutable>]
type TestRecord = {
    Id: int64;
    sProp: string;
    iProp: int;
    oProp: obj;
}


[<Fact>]
let ``test mutable record equality`` () = 
    let someObj:obj = fprintf :> obj
    let r = { Id = 0L; sProp = "someProp"; iProp = 69; oProp = someObj }
    test <@ r = { Id = 0L; sProp = "someProp"; iProp = 69; oProp = someObj } @>
