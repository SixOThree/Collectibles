using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParentChildRelationshipToCollectibleItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                table: "CollectibleItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItems_ParentId",
                table: "CollectibleItems",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectibleItems_CollectibleItems_ParentId",
                table: "CollectibleItems",
                column: "ParentId",
                principalTable: "CollectibleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectibleItems_CollectibleItems_ParentId",
                table: "CollectibleItems");

            migrationBuilder.DropIndex(
                name: "IX_CollectibleItems_ParentId",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "CollectibleItems");
        }
    }
}
