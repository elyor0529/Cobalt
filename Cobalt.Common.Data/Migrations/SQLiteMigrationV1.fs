namespace Common.Common.Data.Migrations

open System.Data.SQLite

type SQLiteMigrationV1(conn) =
    inherit SQLiteMigration(conn, 1L)
    override db.MigrateRun(): unit =
        [db.table "MigrationInfo" 
            [db.fldLong "CurrentVersion"];
        db.insert "MigrationInfo" ["1"];
        db.table "App"
            [db.pkLong "Id";
            db.fldStr "Path";
            db.fldStr "Color";
            db.fldBlob "Icon";
            db.fldStr "Name";];
        db.table "Tag" 
            [db.pkLong "Id";
            db.fldStr "Name";
            db.fldStr "ForegroundColor";
            db.fldStr "BackgroundColor"];
        db.table "_AppTag"
            [db.fldLong "AppId";
            db.fldLong "TagId";
            db.keys ["AppId"; "TagId"];
            db.fk { key = "AppId"; fTable = "App"; fKey = "Id";
                triggers=dict["delete", "cascade"] };
            db.fk { key = "TagId"; fTable = "Tag"; fKey = "Id";
                triggers=dict["delete", "cascade"] }];
        db.table "Reminder"
            [db.pkLong "Id";
            db.fldLong "Offset";
            db.fldLong "ActionType";
            db.fldStr "ActionParam";
            db.fldLong "AlertId";
            db.fk { key = "AlertId"; fTable = "Alert"; fKey = "Id";
                triggers=dict["delete", "cascade"] }];
        db.table "Alert"
            [db.pkLong "Id";
            db.fldLong "MaxDuration";
            db.fldLong "Enabled";
            db.fldLong "ActionType";
            db.fldStr "ActionParam";
            db.fldLong "TimeRangeType";
            db.fldLong "TimeRangeParam1";
            db.fldLong "TimeRangeParam2";
            db.fldLong "EntityType";
            db.fldLong "AppId";
            db.fldLong "TagId";
            db.fk { key = "AppId"; fTable = "App"; fKey = "Id";
                triggers=dict["delete", "cascade"] };
            db.fk { key = "TagId"; fTable = "Tag"; fKey = "Id";
                triggers=dict["delete", "cascade"] }];
        db.table "AppUsage" 
            [db.pkLong "Id";
            db.fldLong "AppId";
            db.fldLong "Start";
            db.fldLong "End";
            db.fldLong "UsageType";
            db.fldLong "StartReason";
            db.fldLong "EndReason";
            db.fk { key = "AppId"; fTable="App"; fKey = "Id";
                triggers=dict["delete", "cascade"]}];
        db.index "AppPathIdx" "App" ["Path"];
        db.index "StartTimestampIdx" "AppUsage" ["Start"; "End"];
        db.index "EndTimestampIdx" "AppUsage" ["End"; "Start"]]
        |> db.exec

