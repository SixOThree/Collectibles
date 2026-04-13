using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentDefinitionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectibleItems_ContentDefinitions_ContentTypeId",
                table: "CollectibleItems");

            migrationBuilder.RenameColumn(
                name: "ContentTypeId",
                table: "CollectibleItems",
                newName: "ContentDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_CollectibleItems_ContentTypeId",
                table: "CollectibleItems",
                newName: "IX_CollectibleItems_ContentDefinitionId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContentDefinitions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "ContentDefinitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefinitionJson",
                table: "ContentDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "ContentDefinitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ContentDefinitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ContentDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentDefinitions_IsActive",
                table: "ContentDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContentDefinitions_Name",
                table: "ContentDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CollectibleItems_ContentDefinitions_ContentDefinitionId",
                table: "CollectibleItems",
                column: "ContentDefinitionId",
                principalTable: "ContentDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectibleItems_ContentDefinitions_ContentDefinitionId",
                table: "CollectibleItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentDefinitions_IsActive",
                table: "ContentDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ContentDefinitions_Name",
                table: "ContentDefinitions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ContentDefinitions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ContentDefinitions");

            migrationBuilder.RenameColumn(
                name: "ContentDefinitionId",
                table: "CollectibleItems",
                newName: "ContentTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_CollectibleItems_ContentDefinitionId",
                table: "CollectibleItems",
                newName: "IX_CollectibleItems_ContentTypeId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContentDefinitions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "ContentDefinitions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefinitionJson",
                table: "ContentDefinitions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "ContentDefinitions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CollectibleItems_ContentDefinitions_ContentTypeId",
                table: "CollectibleItems",
                column: "ContentTypeId",
                principalTable: "ContentDefinitions",
                principalColumn: "Id");
        }
    }
}
