using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Showcases_Attachments_PreviewImageId",
                table: "Showcases");

            migrationBuilder.AddForeignKey(
                name: "FK_Showcases_Attachments_PreviewImageId",
                table: "Showcases",
                column: "PreviewImageId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Showcases_Attachments_PreviewImageId",
                table: "Showcases");

            migrationBuilder.AddForeignKey(
                name: "FK_Showcases_Attachments_PreviewImageId",
                table: "Showcases",
                column: "PreviewImageId",
                principalTable: "Attachments",
                principalColumn: "Id");
        }
    }
}
