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
            db.fldStr "Name";]]
        |> db.exec

