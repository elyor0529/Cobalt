﻿namespace Common.Common.Data.Migrations

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
                triggers=dict["delete", "cascade"] }]]
        |> db.exec

