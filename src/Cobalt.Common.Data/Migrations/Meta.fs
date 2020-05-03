namespace Cobalt.Common.Data.Migrations

module Meta =
    type KeyOptions = AutoIncrement | Normal
    type FieldType = Text | Int | Real | Blob
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
    let integer = field Int
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
    type MigrationBase(ver: int) =
        member _.Version = ver
        abstract member Migrate: SchemaContext -> unit

