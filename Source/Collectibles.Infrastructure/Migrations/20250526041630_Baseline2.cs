using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Baseline2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attachments_Created",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "AcquiredDate",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "EstimatedValue",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CollectibleItems");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CollectibleItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "DeletedBy",
                table: "CollectibleItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "CollectibleItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContentTypeId",
                table: "CollectibleItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentValue",
                table: "CollectibleItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailedDescription",
                table: "CollectibleItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OriginalFilename",
                table: "Attachments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Attachments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "Attachments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileType",
                table: "Attachments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Attachments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

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

            migrationBuilder.CreateTable(
                name: "CollectibleItemCollectibleItem",
                columns: table => new
                {
                    CollectibleItemId = table.Column<long>(type: "bigint", nullable: false),
                    ComponentOfItemId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectibleItemCollectibleItem", x => new { x.CollectibleItemId, x.ComponentOfItemId });
                    table.ForeignKey(
                        name: "FK_CollectibleItemCollectibleItem_CollectibleItems_CollectibleItemId",
                        column: x => x.CollectibleItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectibleItemCollectibleItem_CollectibleItems_ComponentOfItemId",
                        column: x => x.ComponentOfItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LinkInfos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Url = table.Column<string>(type: "varchar(4000)", unicode: false, maxLength: 4000, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Showcases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PreviewImageId = table.Column<long>(type: "bigint", nullable: true),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showcases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Showcases_Attachments_PreviewImageId",
                        column: x => x.PreviewImageId,
                        principalTable: "Attachments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyTerms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyTerms", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "CollectibleItemShowcase",
                columns: table => new
                {
                    CollectibleItemsId = table.Column<long>(type: "bigint", nullable: false),
                    ShowcasesId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectibleItemShowcase", x => new { x.CollectibleItemsId, x.ShowcasesId });
                    table.ForeignKey(
                        name: "FK_CollectibleItemShowcase_CollectibleItems_CollectibleItemsId",
                        column: x => x.CollectibleItemsId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectibleItemShowcase_Showcases_ShowcasesId",
                        column: x => x.ShowcasesId,
                        principalTable: "Showcases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectibleItemTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectibleItemId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectibleItemTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectibleItemTags_CollectibleItems_CollectibleItemId",
                        column: x => x.CollectibleItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectibleItemTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowcaseTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    ShowcaseId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowcaseTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowcaseTags_Showcases_ShowcaseId",
                        column: x => x.ShowcaseId,
                        principalTable: "Showcases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShowcaseTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyVocabularies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    VocabularyId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyVocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxonomyVocabularies_TaxonomyTerms_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "TaxonomyTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxonomyVocabularies_TaxonomyVocabularies_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TaxonomyVocabularies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItems_ContentTypeId",
                table: "CollectibleItems",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentCollectibleItem_CollectibleItemId",
                table: "AttachmentCollectibleItem",
                column: "CollectibleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemCollectibleItem_ComponentOfItemId",
                table: "CollectibleItemCollectibleItem",
                column: "ComponentOfItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemLinkInfo_ExternalReferencesId",
                table: "CollectibleItemLinkInfo",
                column: "ExternalReferencesId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemShowcase_ShowcasesId",
                table: "CollectibleItemShowcase",
                column: "ShowcasesId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemTags_CollectibleItemId",
                table: "CollectibleItemTags",
                column: "CollectibleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectibleItemTags_TagId",
                table: "CollectibleItemTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Showcases_PreviewImageId",
                table: "Showcases",
                column: "PreviewImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowcaseTags_ShowcaseId",
                table: "ShowcaseTags",
                column: "ShowcaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowcaseTags_TagId",
                table: "ShowcaseTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyVocabularies_ParentId",
                table: "TaxonomyVocabularies",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyVocabularies_VocabularyId",
                table: "TaxonomyVocabularies",
                column: "VocabularyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectibleItems_ContentDefinitions_ContentTypeId",
                table: "CollectibleItems",
                column: "ContentTypeId",
                principalTable: "ContentDefinitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectibleItems_ContentDefinitions_ContentTypeId",
                table: "CollectibleItems");

            migrationBuilder.DropTable(
                name: "AttachmentCollectibleItem");

            migrationBuilder.DropTable(
                name: "CollectibleItemCollectibleItem");

            migrationBuilder.DropTable(
                name: "CollectibleItemLinkInfo");

            migrationBuilder.DropTable(
                name: "CollectibleItemShowcase");

            migrationBuilder.DropTable(
                name: "CollectibleItemTags");

            migrationBuilder.DropTable(
                name: "ContentDefinitions");

            migrationBuilder.DropTable(
                name: "ShowcaseTags");

            migrationBuilder.DropTable(
                name: "TaxonomyVocabularies");

            migrationBuilder.DropTable(
                name: "LinkInfos");

            migrationBuilder.DropTable(
                name: "Showcases");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "TaxonomyTerms");

            migrationBuilder.DropIndex(
                name: "IX_CollectibleItems_ContentTypeId",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "ContentTypeId",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "ContentValue",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "DetailedDescription",
                table: "CollectibleItems");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CollectibleItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeletedBy",
                table: "CollectibleItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "CollectibleItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcquiredDate",
                table: "CollectibleItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CollectibleItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "CollectibleItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CollectibleItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedValue",
                table: "CollectibleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CollectibleItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OriginalFilename",
                table: "Attachments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Attachments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "Attachments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileType",
                table: "Attachments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Attachments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_Created",
                table: "Attachments",
                column: "Created");
        }
    }
}
