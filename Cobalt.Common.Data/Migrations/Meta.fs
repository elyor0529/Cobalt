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

    type Table = { name: string; fields: Map<string, Field> }

    type Index = { name: string; table: string; column: string list; }

    type Schema = { tables: Map<string, Table>; indexes: Map<string, Index> }

    type AlterTable =
        | AddColumn of string
        | RemoveColumn of string

    type TableChanges = { added: Table list; removed: string list; altered: AlterTable list }
    type IndexChanges = { added: Index list; removed: string list }
    type SchemaChanges = { table: TableChanges; index: IndexChanges }

    [<AbstractClass>]
    type MigrationBase(ver: int) =
        member _.Version = ver
        abstract member Migrate: Schema -> SchemaChanges

    let noChange = { table = { added = []; removed = []; altered = []}; index = { added = []; removed = [] } }
    let emptySchema = { tables = Map.empty; indexes = Map.empty }

    let table name = { name = name; fields = Map.empty }

    let field fieldType name fns table =
        let fn = List.reduce (>>) fns
        let field = fn { name = name; fieldType = fieldType; unique = false; nullable = false; keyOpt = None }
        { table with fields = Map.add name field table.fields }
    let text = field Text
    let integer = field Int
    let blob = field Blob
    let real = field Real

    let nullable field = { field with nullable = true }
    let unique field = { field with unique = true }
    let primaryKey typ field = { field with keyOpt = Some(typ) }

    let pkAuto = [primaryKey AutoIncrement]


    

