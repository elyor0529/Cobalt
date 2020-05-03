﻿namespace Cobalt.Common.Data.Migrations

open Cobalt.Common.Data.Migrations.Meta

type Migration1(conn) =
    inherit MigrationBase(1, conn)

    override _.DescribeMigration ctx =

        table "Apps"
            |> integer  "Id"                        [pkAuto]
            |> text     "Name"                      []
            |> integer  "Identification_Tag"        []
            |> text     "Identification_Text1"      []
            |> text     "Background"                []
            |> blob     "Icon"                      []
            |> uniques  ["Identification_Tag"; "Identification_Text1"]
            |>> ctx

        table "Tags"
            |> integer  "Id"                        [pkAuto]
            |> text     "Name"                      [unique]
            |> text     "Color"                     []
            |>> ctx

        table "Apps_Tags"
            |> integer  "AppId"                     []
            |> integer  "TagId"                     []
            |> primaryKeys ["AppId"; "TagId"]
            |> foreignKeys ["AppId"] "App" ["Id"]
            |> foreignKeys ["TagId"] "Tag" ["Id"]
            |>> ctx

        table "Sessions"
            |> integer  "Id"                        [pkAuto]
            |> text     "Title"                     []
            |> text     "CmdLine"                   []
            |> integer  "AppId"                     []
            |> foreignKeys ["AppId"] "App" ["Id"]
            |>> ctx

        table "Usages"
            |> integer  "Id"                        [pkAuto]
            |> integer  "Start"                     []
            |> integer  "End"                       []
            |> integer  "SessionId"                 []
            |> foreignKeys ["SessionId"] "Session" ["Id"]
            |>> ctx

        table "SystemEvents"
            |> integer  "Id"                        [pkAuto]
            |> integer  "Timestamp"                 []
            |> integer  "Kind"                      []
            |>> ctx

        table "Alerts"
            |> integer  "Id"                        [pkAuto]
            |> integer  "Target_AppId"              [nullable]
            |> integer  "Target_TagId"              [nullable]
            |> integer  "TimeRange_Tag"             []
            |> integer  "TimeRange_Integer1"        []
            |> integer  "TimeRange_Integer2"        []
            |> integer  "TimeRange_Integer3"        [nullable]
            |> integer  "UsageLimit"                []
            |> integer  "ExceededReaction_Tag"      []
            |> text     "ExceededReaction_Text1"    [nullable]
            |> foreignKeys ["Target_AppId"] "App" ["Id"]
            |> foreignKeys ["Target_TagId"] "Tag" ["Id"]
            |>> ctx

        index "App_Identification"
            "Apps" ["Identification_Tag"; "Identification_Text1"]
            |>> ctx
        index "Usage_StartEnd"
            "Usages" ["Start"; "End"]
            |>> ctx
        index "Usage_EndStart"
            "Usages" ["End"; "Start"]
            |>> ctx

