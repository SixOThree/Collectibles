using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentMigrationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMigrated",
                table: "Attachments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigrationDate",
                table: "Attachments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMigrated",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "MigrationDate",
                table: "Attachments");
        }
    }
}
