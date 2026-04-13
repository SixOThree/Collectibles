using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MhtmlPath",
                table: "LinkCaches");

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ToName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CcEmails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BccEmails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    IsHtml = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MessageId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TemplateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_ScheduledFor",
                table: "EmailLogs",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_SentAt",
                table: "EmailLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_Status",
                table: "EmailLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_Status_ScheduledFor_Priority_Created",
                table: "EmailLogs",
                columns: new[] { "Status", "ScheduledFor", "Priority", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_ToEmail",
                table: "EmailLogs",
                column: "ToEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.AddColumn<string>(
                name: "MhtmlPath",
                table: "LinkCaches",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
