namespace Cobalt.Common.Data.Migrations

open Microsoft.Data.Sqlite
open Dapper
open System

module Meta =
    type KeyOptions = AutoIncrement | Normal
    type FieldType = Text | Integer | Real | Blob
    type Field = {
        name: string;
        fieldType: FieldType;
        unique: bool;
        nullable: bool;
        keyOpt: option<KeyOptions>
    }
    type ForeignKey = { cols: string list; fTable: string; fCols: string list }

    type Table = { name: string; fields: Map<string, Field>; uniques: string list list; primaryKeys: string list list; foreignKeys: ForeignKey list }

    type Index = { name: string; table: string; columns: string list; }

    type Schema = { tables: Map<string, Table>; indexes: Map<string, Index> }

    type AlterTable =
        | AddColumn of string
        | RemoveColumn of string

    type TableChanges = { added: Table list; removed: string list; altered: AlterTable list }
    type IndexChanges = { added: Index list; removed: string list }
    type SchemaChanges = {
        table: TableChanges;
        index: IndexChanges
    }
    let emptyChanges = { table = { added = []; removed = []; altered = []}; index = { added = []; removed = [] } }
    let emptySchema = { tables = Map.empty; indexes = Map.empty }

    let table name = { name = name; fields = Map.empty; uniques = []; primaryKeys = []; foreignKeys = [] }
    let index name table cols = { name = name; table = table; columns = cols }

    let field fieldType name fns table =
        let fn = List.reduce (>>) (id :: fns)
        let field = fn { name = name; fieldType = fieldType; unique = false; nullable = false; keyOpt = None }
        { table with fields = Map.add name field table.fields }
    let text = field Text
    let integer = field Integer
    let blob = field Blob
    let real = field Real

    let uniques cols table = { table with uniques = cols :: table.uniques }
    let primaryKeys cols table = { table with primaryKeys = cols :: table.primaryKeys }
    let foreignKeys cols fTable fCols table = { table with foreignKeys = { cols = cols; fTable = fTable; fCols = fCols } :: table.foreignKeys }

    let nullable field = { field with nullable = true }
    let unique (field: Field) = { field with unique = true }
    let primaryKey typ field = { field with keyOpt = Some(typ) }

    let pkAuto = primaryKey AutoIncrement

    type SchemaContext(schema: Schema) =
        member val Schema = schema
        member val Changes = emptyChanges with get, set

        static member (|>>) (table: Table, x: SchemaContext) =
            x.Changes <- { x.Changes with table = { x.Changes.table with added = table :: x.Changes.table.added } }

        static member (|>>) (index: Index, x: SchemaContext) =
            x.Changes <- { x.Changes with index = { x.Changes.index with added = index :: x.Changes.index.added } }

    [<AbstractClass>]
    type MigrationBase(ver: int, conn: SqliteConnection) =

        let seperatedString s v = String.concat s (v |> Seq.filter (String.IsNullOrEmpty >> not))
        let commaSep v = seperatedString "," v
        let spaceSep v = seperatedString " " v

        let fieldSql (fld: Field) =
            let name = fld.name
            let fldType = string fld.fieldType
            let nullable = if fld.nullable then "" else "not null"
            let unique = if fld.unique then "unique" else ""
            let keyopts =
                match fld.keyOpt with
                    | None -> ""
                    | Some AutoIncrement -> "primary key autoincrement"
                    | Some Normal -> "primary key"
            spaceSep [name; fldType; nullable; unique; keyopts]

        let uniquesSql cols = sprintf "unique (%s)" (commaSep cols)
        let primaryKeysSql cols = sprintf "primary key (%s)" (commaSep cols)
        let foreignKeySql fk = sprintf "foreign key(%s) references %s(%s)" (commaSep fk.cols) fk.fTable (commaSep fk.fCols)


        let createTableSql tbl =
            let fields = tbl.fields |> Map.toSeq |> Seq.map snd |> Seq.map fieldSql
            let uniques = tbl.uniques |> Seq.map uniquesSql
            let pks = tbl.primaryKeys |> Seq.map primaryKeysSql
            let fks = tbl.foreignKeys |> Seq.map foreignKeySql
            sprintf "create table %s (%s);" tbl.name ([fields; pks; uniques; fks;] |> List.map commaSep |> commaSep)
        let removeTableSql tbl = failwith "not implemented"
        let alterTableSql tbl = failwith "not implemented"

        let createIndexSql idx = sprintf "create index %s on %s(%s)" idx.name idx.table (commaSep idx.columns)
        let removeIndexSql idx = failwith "not implemented"

        member _.Version = ver
        abstract member DescribeMigration: SchemaContext -> unit
        member x.Migrate prevSchema =
            let ctx = SchemaContext(prevSchema)
            x.DescribeMigration ctx

            if ver = 1 then
                table "Migrations"
                    |> integer "Version" [pkAuto]
                    |>> ctx

            use trans = conn.BeginTransaction()

            List.concat [
                ctx.Changes.table.added |> List.map createTableSql;
                ctx.Changes.table.removed |> List.map removeTableSql;
                ctx.Changes.table.altered |> List.map alterTableSql;

                ctx.Changes.index.added |> List.map createIndexSql;
                ctx.Changes.index.removed |> List.map removeIndexSql;
            ] |> List.iter
                (fun x -> conn.Execute(x, transaction = trans) |> ignore)

            conn.Execute("insert into Migrations values (@Version)", {| Version = ver |}) |> ignore

            trans.Commit()

