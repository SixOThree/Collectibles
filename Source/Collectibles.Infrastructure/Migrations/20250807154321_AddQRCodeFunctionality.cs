using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQRCodeFunctionality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "QRCodeId",
                table: "CollectibleItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QRCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CollectibleItemId = table.Column<long>(type: "bigint", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScanCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastScannedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QRCodes_CollectibleItems_CollectibleItemId",
                        column: x => x.CollectibleItemId,
                        principalTable: "CollectibleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_Code",
                table: "QRCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_CollectibleItemId",
                table: "QRCodes",
                column: "CollectibleItemId",
                unique: true,
                filter: "[CollectibleItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_CreatedBy",
                table: "QRCodes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_Status",
                table: "QRCodes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_Status_CreatedBy",
                table: "QRCodes",
                columns: new[] { "Status", "CreatedBy" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QRCodes");

            migrationBuilder.DropColumn(
                name: "QRCodeId",
                table: "CollectibleItems");
        }
    }
}
