using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitAttachmentContentAndPreview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create AttachmentContents table
            migrationBuilder.CreateTable(
                name: "AttachmentContents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentContents_Attachments_Id",
                        column: x => x.Id,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create AttachmentPreviews table
            migrationBuilder.CreateTable(
                name: "AttachmentPreviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PreviewThumbnail = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentPreviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentPreviews_Attachments_Id",
                        column: x => x.Id,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Migrate existing data from Attachments to AttachmentContents
            migrationBuilder.Sql(@"
                INSERT INTO AttachmentContents (Id, Content)
                SELECT Id, Content
                FROM Attachments
                WHERE Content IS NOT NULL
            ");

            // Migrate existing data from Attachments to AttachmentPreviews
            migrationBuilder.Sql(@"
                INSERT INTO AttachmentPreviews (Id, PreviewThumbnail)
                SELECT Id, PreviewThumbnail
                FROM Attachments
                WHERE PreviewThumbnail IS NOT NULL
            ");

            // Drop the Content and PreviewThumbnail columns from Attachments table
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "PreviewThumbnail",
                table: "Attachments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add the Content and PreviewThumbnail columns back to Attachments table
            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "Attachments",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PreviewThumbnail",
                table: "Attachments",
                type: "varbinary(max)",
                nullable: true);

            // Migrate data back from AttachmentContents to Attachments
            migrationBuilder.Sql(@"
                UPDATE a
                SET a.Content = ac.Content
                FROM Attachments a
                INNER JOIN AttachmentContents ac ON a.Id = ac.Id
            ");

            // Migrate data back from AttachmentPreviews to Attachments
            migrationBuilder.Sql(@"
                UPDATE a
                SET a.PreviewThumbnail = ap.PreviewThumbnail
                FROM Attachments a
                INNER JOIN AttachmentPreviews ap ON a.Id = ap.Id
            ");

            // Drop the AttachmentContents table
            migrationBuilder.DropTable(
                name: "AttachmentContents");

            // Drop the AttachmentPreviews table
            migrationBuilder.DropTable(
                name: "AttachmentPreviews");
        }
    }
}