namespace Common.Common.Data.Migrations

type IDbMigration =
    abstract member Version: int64
    abstract member Run: unit -> unit

type IDbMigrator =
    abstract member Migrations: IDbMigration[]
    abstract member Migrate: unit -> unit
