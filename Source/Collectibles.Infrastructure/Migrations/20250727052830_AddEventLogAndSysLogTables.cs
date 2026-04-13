using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLogAndSysLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EntityId = table.Column<long>(type: "bigint", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ProcessName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ThreadId = table.Column<int>(type: "int", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_Action",
                table: "EventLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_EntityId",
                table: "EventLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_EntityType",
                table: "EventLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_EntityType_EntityId",
                table: "EventLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_Timestamp",
                table: "EventLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_UserId",
                table: "EventLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SysLogs_Category",
                table: "SysLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SysLogs_CorrelationId",
                table: "SysLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SysLogs_Level",
                table: "SysLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SysLogs_Level_Timestamp",
                table: "SysLogs",
                columns: new[] { "Level", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SysLogs_Timestamp",
                table: "SysLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLogs");

            migrationBuilder.DropTable(
                name: "SysLogs");
        }
    }
}
