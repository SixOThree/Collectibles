using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditAddPreviewAttemptTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PreviewAttemptedAt",
                table: "Attachments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviewAttemptedAt",
                table: "Attachments");
        }
    }
}
