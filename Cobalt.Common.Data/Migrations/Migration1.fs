namespace Cobalt.Common.Data.Migrations

open Cobalt.Common.Data.Migrations.Meta

type Migration1() =
    inherit MigrationBase(1)

    override _.Migrate schema =
        noChange
