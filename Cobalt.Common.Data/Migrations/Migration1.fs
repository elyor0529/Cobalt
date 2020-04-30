namespace Cobalt.Common.Data.Migrations

open Cobalt.Common.Data.Migrations.Meta

type Migration1() =
    inherit MigrationBase(1)

    override _.Migrate ctx =
        table "Apps"
            |> integer "Id" pkAuto
            |> ctx.create

        table "AppUsage"
            |> integer "Id" pkAuto
            |> ctx.create



