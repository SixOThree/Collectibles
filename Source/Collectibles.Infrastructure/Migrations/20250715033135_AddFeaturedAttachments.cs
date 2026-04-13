using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturedAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create new table
            migrationBuilder.CreateTable(
                name: "CollectibleItemAttachments",
                columns: table => new
                {
                    CollectibleItemId = table.Column<long>(type: "bigint", nullable: false),
                    AttachmentId = table.Column<long>(type: "bigint", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FeaturedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectibleItemAttachments", x => new { x.CollectibleItemId, x.AttachmentId });
                    table.ForeignKey(
                        name: "FK_CollectibleItemAttachments_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectibleItemAttachments_CollectibleItems_CollectibleItemId",
                        column: x => x.CollectibleItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Copy existing data from old table to new table
            migrationBuilder.Sql(@"
                INSERT INTO CollectibleItemAttachments (CollectibleItemId, AttachmentId, IsFeatured, DisplayOrder)
                SELECT CollectibleItemId, AttachmentsId, 0, 0
                FROM AttachmentCollectibleItem
            ");

            // Drop old table
            migrationBuilder.DropTable(
                name: "AttachmentCollectibleItem");

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemAttachments_AttachmentId",
                table: "CollectibleItemAttachments",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemAttachments_CollectibleItemId_IsFeatured",
                table: "CollectibleItemAttachments",
                columns: new[] { "CollectibleItemId", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemAttachments_IsFeatured",
                table: "CollectibleItemAttachments",
                column: "IsFeatured");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectibleItemAttachments");

            migrationBuilder.CreateTable(
                name: "AttachmentCollectibleItem",
                columns: table => new
                {
                    AttachmentsId = table.Column<long>(type: "bigint", nullable: false),
                    CollectibleItemId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentCollectibleItem", x => new { x.AttachmentsId, x.CollectibleItemId });
                    table.ForeignKey(
                        name: "FK_AttachmentCollectibleItem_Attachments_AttachmentsId",
                        column: x => x.AttachmentsId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttachmentCollectibleItem_CollectibleItems_CollectibleItemId",
                        column: x => x.CollectibleItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentCollectibleItem_CollectibleItemId",
                table: "AttachmentCollectibleItem",
                column: "CollectibleItemId");
        }
    }
}
