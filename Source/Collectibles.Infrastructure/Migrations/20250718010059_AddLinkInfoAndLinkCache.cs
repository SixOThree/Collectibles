using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkInfoAndLinkCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectibleItemLinkInfo");

            migrationBuilder.DropColumn(
                name: "AdditionalInfo",
                table: "LinkInfos");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "LinkInfos");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "LinkInfos",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(4000)",
                oldUnicode: false,
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "LinkInfos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "LinkInfos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CollectibleItemId",
                table: "LinkInfos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "LinkInfos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LinkCaches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LinkInfoId = table.Column<long>(type: "bigint", nullable: false),
                    CachedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CachedContentPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ScreenshotPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MhtmlPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkCaches_LinkInfos_LinkInfoId",
                        column: x => x.LinkInfoId,
                        principalTable: "LinkInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkInfos_CollectibleItemId",
                table: "LinkInfos",
                column: "CollectibleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkCaches_LinkInfoId",
                table: "LinkCaches",
                column: "LinkInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_LinkInfos_CollectibleItems_CollectibleItemId",
                table: "LinkInfos",
                column: "CollectibleItemId",
                principalTable: "CollectibleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LinkInfos_CollectibleItems_CollectibleItemId",
                table: "LinkInfos");

            migrationBuilder.DropTable(
                name: "LinkCaches");

            migrationBuilder.DropIndex(
                name: "IX_LinkInfos_CollectibleItemId",
                table: "LinkInfos");

            migrationBuilder.DropColumn(
                name: "CollectibleItemId",
                table: "LinkInfos");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "LinkInfos");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "LinkInfos",
                type: "varchar(4000)",
                unicode: false,
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "LinkInfos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "LinkInfos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalInfo",
                table: "LinkInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "LinkInfos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollectibleItemLinkInfo",
                columns: table => new
                {
                    CollectibleItemId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalReferencesId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectibleItemLinkInfo", x => new { x.CollectibleItemId, x.ExternalReferencesId });
                    table.ForeignKey(
                        name: "FK_CollectibleItemLinkInfo_CollectibleItems_CollectibleItemId",
                        column: x => x.CollectibleItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectibleItemLinkInfo_LinkInfos_ExternalReferencesId",
                        column: x => x.ExternalReferencesId,
                        principalTable: "LinkInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemLinkInfo_ExternalReferencesId",
                table: "CollectibleItemLinkInfo",
                column: "ExternalReferencesId");
        }
    }
}
