using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectibleItemRelatedTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectibleItemRelatedTags",
                columns: table => new
                {
                    CollectibleItemId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectibleItemRelatedTags", x => new { x.CollectibleItemId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CollectibleItemRelatedTags_CollectibleItems_CollectibleItemId",
                        column: x => x.CollectibleItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectibleItemRelatedTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemRelatedTags_TagId",
                table: "CollectibleItemRelatedTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectibleItemRelatedTags");
        }
    }
}
