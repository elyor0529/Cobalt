﻿using System.Data;

namespace Cobalt.Common.Data.Migration.Sqlite
{
    public class SqliteMigrationV1 : SqliteMigrationBase
    {
        public SqliteMigrationV1(IDbConnection connection) : base(connection)
        {
        }

        public override int Order { get; } = 1;

        public override void ExecuteMigration()
        {
            ExecuteSql(
                Table("Migrations",
                    Field("LatestMigration", Integer())),
                Insert("Migrations", "1"),
                Table("App",
                    Field("Id", Integer(), PkAutoInc()),
                    Field("Name", Text()),
                    Field("Path", Text(), NotNullUnique())),
                Table("Interaction",
                    Field("Id", Integer())),
                Table("Tag",
                    Field("Id", Integer(), PkAutoInc()),
                    Field("Name", Text(), NotNullUnique())),
                Table("AppTag",
                    Field("AppId", Integer()),
                    Field("TagId", Integer()),
                    Key("AppId, TagId"),
                    ForeignKey("AppId", "App(Id)", "delete set null"),
                    ForeignKey("TagId", "Tag(Id)", "delete set null")),
                Table("AppUsage",
                    Field("Id", Integer(), PkAutoInc()),
                    Field("AppId", Integer()),
                    Field("UsageType", Integer()),
                    Field("StartTimestamp", Integer()),
                    Field("EndTimestamp", Integer()),
                    Field("UsageStartReason", Integer()),
                    Field("UsageEndReason", Integer()),
                    ForeignKey("AppId", "App(Id)")),
                Index("AppPathIdx", "App(Path)"),
                Index("InteractionIdx", "Interaction(Id)"),
                Index("StartTimestampIdx", "AppUsage(StartTimestamp, EndTimestamp)"),
                Index("EndTimestampIdx", "AppUsage(EndTimestamp, StartTimestamp)"));
        }
    }
}