module General

open Xunit
open FsUnit.Xunit
open Swensen.Unquote
open Microsoft.FSharp.Reflection
open Cobalt.Common.Data.Entities
open Microsoft.Data.Sqlite
open System.Reflection
open System
open Cobalt.Common.Data.Migrations.Meta
open Cobalt.Common.Data.Migrations
open Cobalt.Common.Data.Migrations.Meta
open Microsoft.Data.Sqlite
open System.Data
open Dapper
open System.IO

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

[<AbstractClass>]
type PropertyMeta (prop: PropertyInfo, ord: int) =
    member x.withType includeType str = if includeType then prop.DeclaringType.Name + "_" + str else str

    abstract member Name: string
    default _.Name = prop.Name

    member val Ordinal = ord
    member val PropertyReader = FSharpValue.PreComputeRecordFieldReader prop

    abstract member UsedOrdinals: unit -> int
    default _.UsedOrdinals () = 1

    abstract member Read: bool -> IDataReader -> obj
    abstract member Write: obj -> SqliteParameterCollection -> unit
    default x.Write object parameters = 
        let value = x.PropertyReader object
        parameters.AddWithValue(x.Name, value) |> ignore

type EmptyPropertyMeta (prop, ord) =
    inherit PropertyMeta(prop, ord)

    override x.UsedOrdinals () = 0

    override x.Read includeType data = null
    override x.Write object parameters = ()

type NormalPropertyMeta (prop, ord) =
    inherit PropertyMeta(prop, ord)

    override x.Read includeType data = 
        data.GetValue(data.GetOrdinal(x.withType includeType x.Name))

type BlobPropertyMeta (prop, ord) =
    inherit PropertyMeta(prop, ord)

    override x.Read includeType data = 
        let v = data.GetValue(data.GetOrdinal(x.withType includeType x.Name)) :?> byte[]
        box (new MemoryStream(v))

    default x.Write object parameters = 
        let buffer = Span<byte>()
        let value = x.PropertyReader object :?> Stream
        value.Read(buffer) |> ignore
        parameters.AddWithValue(x.Name, buffer.ToArray()) |> ignore

type NavigationPropertyMeta (prop, ord) = 
    inherit PropertyMeta(prop, ord)
    let navProp = FSharpValue.PreComputeRecordConstructor prop.PropertyType

    override _.Name = prop.Name + "Id"

    override x.Read includeType data = 
        let navId = data.GetInt64(data.GetOrdinal(x.withType includeType x.Name))
        navProp [| navId |]

[<StructuralComparison>]
[<StructuralEquality>]
type DbType = Integer | Real | Text | Blob | Nav of string

let isNav t =  FSharpType.IsRecord t && t.GetProperty("Id") <> null

let getDbType (t: Type) =
    match t with
        | t when t = typeof<string> -> Some DbType.Text
        | t when t = typeof<int8> -> Some DbType.Integer
        | t when t = typeof<int16> -> Some DbType.Integer
        | t when t = typeof<int32> -> Some DbType.Integer
        | t when t = typeof<int64> -> Some DbType.Integer
        | t when t = typeof<TimeSpan> -> Some DbType.Integer
        | t when t = typeof<DateTime> -> Some DbType.Integer
        | t when t.IsEnum -> Some DbType.Integer
        | t when t = typeof<float32> -> Some DbType.Real
        | t when t = typeof<float> -> Some DbType.Real
        | t when t = typeof<byte[]> -> Some DbType.Blob
        | t when t.IsSubclassOf typeof<Stream> -> Some DbType.Blob
        | t when t = typeof<Lazy<App seq>> -> None
        | t when t = typeof<Lazy<Tag seq>> -> None
        | t when isNav t -> Some (DbType.Nav t.Name)
        | _ -> failwith "Unsupported Type"

let dbTypes = [| DbType.Integer; DbType.Real; DbType.Text; DbType.Blob; DbType.Nav "App"; DbType.Nav "Tag" |]

type UnionPropertyMeta (prop, ord) = 
    inherit PropertyMeta(prop, ord)

    let cases = FSharpType.GetUnionCases prop.PropertyType
    let caseCtors = cases |> Array.map FSharpValue.PreComputeUnionConstructor
    let tag = FSharpValue.PreComputeUnionTagReader prop.PropertyType

    let typeName t (n:int) =
        match t with
            | DbType.Integer -> "Integer" + string n
            | DbType.Real -> "Real" + string n
            | DbType.Text -> "Text" + string n
            | DbType.Blob -> "Blob" + string n
            | DbType.Nav i -> i + "Id"

    let props =
        cases
        |> Array.map (fun case ->
            case.GetFields() |> Array.map (fun fld -> (fld, getDbType fld.PropertyType)))
        |> Array.map (fun x -> x |> Array.choose (fun (y,z) -> z |> Option.map (fun g -> (y ,g))))
        |> Array.map (Array.sortBy snd)

    let propCounts =
        props
        |> Array.map (Array.countBy snd)
        |> Array.map Map.ofArray

    let propTotal =
        propCounts
        |> Array.collect Map.toArray
        |> Array.groupBy fst
        |> Array.map (fun x -> (fst x, (snd x) |> Array.map snd |> Array.max ))

    let propTotalMap = propTotal |> Map.ofArray

    let ii = 0
    let ir = ii + defaultArg (Map.tryFind DbType.Integer propTotalMap) 0
    let is = ir + defaultArg (Map.tryFind DbType.Real propTotalMap) 0
    let ib = is + defaultArg (Map.tryFind DbType.Text propTotalMap) 0

    override _.UsedOrdinals () = propTotal |> Array.fold (fun s v -> s + snd v) 1

    override x.Read includeType data =
        let tag = data.GetInt64(data.GetOrdinal(x.withType includeType (sprintf "%s_Tag" x.Name))) |> int
        let caseCtor = caseCtors.[tag]
        let caseProps = props.[tag]

        let mutable i = ii
        let mutable r = ir
        let mutable s = is
        let mutable b = ib

        let propVals = caseProps |> Array.map (fun h ->
            match snd h with
            | DbType.Integer -> i <- i + 1; data.GetInt64(data.GetOrdinal(x.withType includeType (sprintf "%s_Integer%d" x.Name i))) |> box
            | DbType.Real -> r <- r + 1; data.GetDouble(data.GetOrdinal(x.withType includeType (sprintf "%s_Real%d" x.Name r))) |> box
            | DbType.Text -> s <- s + 1; data.GetString(data.GetOrdinal(x.withType includeType (sprintf "%s_Text%d" x.Name s))) |> box
            | DbType.Blob -> b <- b + 1; data.GetInt64(data.GetOrdinal(x.withType includeType (sprintf "%s_Blob%d" x.Name b))) |> box
            | DbType.Nav t -> data.GetInt64(data.GetOrdinal(sprintf "%s_%sId" x.Name t)) |> box)
        caseCtor propVals

    override x.Write object parameters = 
        let value = x.PropertyReader object
        let tagVal = tag value
        parameters.AddWithValue(sprintf "%s_Tag" x.Name, tagVal) |> ignore
        
        props.[tagVal]
            |> Array.map (fun x -> ((fst x).GetValue(value) , snd x))
            |> Array.groupBy snd
            |> Array.iter (fun (_, y) -> Array.iteri (fun i v -> parameters.AddWithValue(sprintf "%s_%s" x.Name (typeName (snd v) (i+1)), fst v) |> ignore) y)

let getReadWriter<'a> () =
    let fnt = typeof<'a>
    let typ = FSharpValue.PreComputeRecordConstructor fnt
    let fields = FSharpType.GetRecordFields fnt |> Array.fold (fun metas prop -> 
        match prop.PropertyType with
            | t when FSharpType.IsRecord t && t.GetProperty("Id") <> null ->
                let p = NavigationPropertyMeta(prop, fst metas) :> PropertyMeta;
                (p.UsedOrdinals() + (fst metas), p :: (snd metas))
            | t when FSharpType.IsUnion t ->
                let p = UnionPropertyMeta(prop, fst metas) :> PropertyMeta;
                (p.UsedOrdinals() + (fst metas), p :: (snd metas))
            | t when t.Name = "Lazy`1" && t.GenericTypeArguments.[0].Name = "IEnumerable`1" && isNav t.GenericTypeArguments.[0].GenericTypeArguments.[0] ->
                let p = EmptyPropertyMeta(prop, fst metas) :> PropertyMeta;
                (p.UsedOrdinals() + (fst metas), p :: (snd metas))
            | t when t.IsSubclassOf typeof<Stream> ->
                let p = BlobPropertyMeta(prop, fst metas) :> PropertyMeta;
                (p.UsedOrdinals() + (fst metas), p :: (snd metas))
            | _ -> 
                let p = NormalPropertyMeta(prop, fst metas) :> PropertyMeta;
                (p.UsedOrdinals() + (fst metas), p :: (snd metas))) (0, []) |> snd |> List.rev |> Array.ofList

    let read offset data = fields |> Array.map (fun x -> x.Read offset data) |> typ :?> 'a
    let write (object:'a) parameters = fields |> Array.iter (fun x -> x.Write object parameters)
    (read, write)

let sqlFlds<'a> sch =
    let flds = (sch.tables.Item typeof<'a>.Name).fields |> Map.toArray |> Array.map fst
    flds |> Array.map (sprintf "@%s") |> String.concat ","
    

let ``fun stuff`` () =
    let (read, write) = getReadWriter<App>()
    use conn = new SqliteConnection("Data Source=:memory:")
    conn.Open()
    test <@ conn.State.HasFlag ConnectionState.Open @>

    let mig = Migrator(conn) :> IMigrator
    let sch = mig.Migrate()

    let app = { Id = 4L; Name = "Chrome"; Identification = UWP "Main"; Background = "black"; Icon = new MemoryStream([|1uy;2uy;3uy;4uy|]); Tags = null }

    let cmd = conn.CreateCommand()
    cmd.CommandText <- sprintf "insert into app values (%s)" (sqlFlds<App> sch)
    write app cmd.Parameters
    cmd.ExecuteNonQuery() |> ignore

    let v = conn.ExecuteScalar("select count(*) from app") :?> int64
    test <@ v = 1L @>

    let reader = conn.ExecuteReader("select * from app")
    test <@ reader.Read() @>
    let app2 = read false reader
    let s1 = Span<byte>()
    let s2 = Span<byte>()
    let r1 = app.Icon.Read(s1)
    let r2 = app2.Icon.Read(s2)
    let ss1 = s1.ToArray()
    let ss2 = s2.ToArray()
    test <@ r1 = r2 && ss1 = ss2 @>
    test <@ { app with Icon = null } = { app2 with Icon = null } @>

    let alert = {
        Id = 9L;
        Target = App app;
        TimeRange = Repeated (RepeatType.Monthly, TimeSpan.FromHours(8.0), TimeSpan.FromHours(10.0));
        UsageLimit = TimeSpan.FromMinutes(3.0);
        ExceededReaction = Message "bookies"}

    let (read2, write2) = getReadWriter<Alert>()
    let cmd2 = conn.CreateCommand()
    cmd2.CommandText <- sprintf "insert into alert values (%s)" (sqlFlds<Alert> sch)
    write2 alert cmd2.Parameters
    cmd2.ExecuteNonQuery() |> ignore

    let v = conn.ExecuteScalar("select count(*) from alert") :?> int64
    test <@ v = 1L @>

[<Fact>]
let ``test mutable record equality`` () = 
    let someObj:obj = fprintf :> obj
    let r = { Id = 0L; sProp = "someProp"; iProp = 69; oProp = someObj }
    test <@ r = { Id = 0L; sProp = "someProp"; iProp = 69; oProp = someObj } @>
