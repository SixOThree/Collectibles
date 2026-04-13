using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalAndShowcaseToContentDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentDefinitions_Name",
                table: "ContentDefinitions");

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "ContentDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ShowcaseId",
                table: "ContentDefinitions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentDefinitions_IsGlobal",
                table: "ContentDefinitions",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_ContentDefinitions_Name_ShowcaseId",
                table: "ContentDefinitions",
                columns: new[] { "Name", "ShowcaseId" },
                unique: true,
                filter: "[ShowcaseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContentDefinitions_ShowcaseId",
                table: "ContentDefinitions",
                column: "ShowcaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentDefinitions_Showcases_ShowcaseId",
                table: "ContentDefinitions",
                column: "ShowcaseId",
                principalTable: "Showcases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentDefinitions_Showcases_ShowcaseId",
                table: "ContentDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ContentDefinitions_IsGlobal",
                table: "ContentDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ContentDefinitions_Name_ShowcaseId",
                table: "ContentDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ContentDefinitions_ShowcaseId",
                table: "ContentDefinitions");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "ContentDefinitions");

            migrationBuilder.DropColumn(
                name: "ShowcaseId",
                table: "ContentDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_ContentDefinitions_Name",
                table: "ContentDefinitions",
                column: "Name",
                unique: true);
        }
    }
}
