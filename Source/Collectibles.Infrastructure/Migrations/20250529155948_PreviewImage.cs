using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreviewImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PreviewImageId",
                table: "CollectibleItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItems_PreviewImageId",
                table: "CollectibleItems",
                column: "PreviewImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectibleItems_Attachments_PreviewImageId",
                table: "CollectibleItems",
                column: "PreviewImageId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectibleItems_Attachments_PreviewImageId",
                table: "CollectibleItems");

            migrationBuilder.DropIndex(
                name: "IX_CollectibleItems_PreviewImageId",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "PreviewImageId",
                table: "CollectibleItems");
        }
    }
}
