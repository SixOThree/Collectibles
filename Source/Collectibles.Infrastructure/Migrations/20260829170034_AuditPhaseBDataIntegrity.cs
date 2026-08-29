using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditPhaseBDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCodeId",
                table: "CollectibleItems");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ZipUploadJobs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "ZipUploadJobs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentItemName",
                table: "ZipUploadJobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ZipUploadJobs",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SiteConfigurations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SiteConfigurations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SiteConfigurations",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "ShowcaseShareTokens",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ShowcaseShareTokens",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Showcases",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "RequestLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "RequestLogs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "RequestLogs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Scheme",
                table: "RequestLogs",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResponseContentType",
                table: "RequestLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestId",
                table: "RequestLogs",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QueryString",
                table: "RequestLogs",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "RequestLogs",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "RequestLogs",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "RequestLogs",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Host",
                table: "RequestLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExceptionType",
                table: "RequestLogs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "RequestLogs",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "RequestLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PasswordHistories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "PasswordHistories",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "LinkCaches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ContentDefinitions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CollectibleItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Attachments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            // The unique indexes below turn invariants that were previously only enforced
            // in application memory into database constraints. Existing data may already
            // violate them (that is precisely the defect), so reconcile first.

            // Fold duplicate live tags onto the lowest-id survivor, repointing both
            // junction tables before soft-deleting the losers.
            migrationBuilder.Sql(@"
                ;WITH Ranked AS (
                    SELECT Id, Name,
                           MIN(Id) OVER (PARTITION BY Name) AS KeepId
                    FROM Tags
                    WHERE Deleted IS NULL
                )
                UPDATE cit
                SET cit.TagId = r.KeepId
                FROM CollectibleItemTags cit
                INNER JOIN Ranked r ON r.Id = cit.TagId
                WHERE r.Id <> r.KeepId;");

            migrationBuilder.Sql(@"
                ;WITH Ranked AS (
                    SELECT Id, Name,
                           MIN(Id) OVER (PARTITION BY Name) AS KeepId
                    FROM Tags
                    WHERE Deleted IS NULL
                )
                UPDATE st
                SET st.TagId = r.KeepId
                FROM ShowcaseTags st
                INNER JOIN Ranked r ON r.Id = st.TagId
                WHERE r.Id <> r.KeepId;");

            // Junction rows may now be duplicated after the repoint; collapse them.
            migrationBuilder.Sql(@"
                ;WITH Dupes AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY CollectibleItemId, TagId ORDER BY Id) AS rn
                    FROM CollectibleItemTags
                )
                DELETE FROM CollectibleItemTags WHERE Id IN (SELECT Id FROM Dupes WHERE rn > 1);");

            migrationBuilder.Sql(@"
                ;WITH Dupes AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY ShowcaseId, TagId ORDER BY Id) AS rn
                    FROM ShowcaseTags
                )
                DELETE FROM ShowcaseTags WHERE Id IN (SELECT Id FROM Dupes WHERE rn > 1);");

            migrationBuilder.Sql(@"
                ;WITH Dupes AS (
                    SELECT Id, Deleted,
                           ROW_NUMBER() OVER (PARTITION BY Name ORDER BY Id) AS rn
                    FROM Tags
                    WHERE Deleted IS NULL
                )
                UPDATE Tags SET Deleted = SYSUTCDATETIME()
                WHERE Id IN (SELECT Id FROM Dupes WHERE rn > 1);");

            // Keep the most recently modified row per configuration key.
            migrationBuilder.Sql(@"
                ;WITH Dupes AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY [Key] ORDER BY LastModified DESC, Id DESC) AS rn
                    FROM SiteConfigurations
                )
                DELETE FROM SiteConfigurations WHERE Id IN (SELECT Id FROM Dupes WHERE rn > 1);");

            // Duplicate share tokens are astronomically unlikely but the constraint must
            // be creatable regardless; retire the later duplicates.
            migrationBuilder.Sql(@"
                ;WITH Dupes AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY Token ORDER BY Id) AS rn
                    FROM ShowcaseShareTokens
                )
                UPDATE ShowcaseShareTokens SET IsActive = 0, Deleted = SYSUTCDATETIME()
                WHERE Id IN (SELECT Id FROM Dupes WHERE rn > 1);");

            // Password history rows whose user no longer exists would block the new
            // foreign key.
            migrationBuilder.Sql(@"
                DELETE FROM PasswordHistories
                WHERE UserId NOT IN (SELECT Id FROM AspNetUsers);");

            migrationBuilder.CreateIndex(
                name: "IX_ZipUploadJobs_Status",
                table: "ZipUploadJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ZipUploadJobs_UserId_Created",
                table: "ZipUploadJobs",
                columns: new[] { "UserId", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true,
                filter: "[Deleted] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SiteConfigurations_Key",
                table: "SiteConfigurations",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowcaseShareTokens_Token",
                table: "ShowcaseShareTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_StatusCode_Timestamp",
                table: "RequestLogs",
                columns: new[] { "StatusCode", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Timestamp",
                table: "RequestLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_UserId_Timestamp",
                table: "RequestLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId_CreatedAt",
                table: "PasswordHistories",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordHistories_AspNetUsers_UserId",
                table: "PasswordHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordHistories_AspNetUsers_UserId",
                table: "PasswordHistories");

            migrationBuilder.DropIndex(
                name: "IX_ZipUploadJobs_Status",
                table: "ZipUploadJobs");

            migrationBuilder.DropIndex(
                name: "IX_ZipUploadJobs_UserId_Created",
                table: "ZipUploadJobs");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_SiteConfigurations_Key",
                table: "SiteConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_ShowcaseShareTokens_Token",
                table: "ShowcaseShareTokens");

            migrationBuilder.DropIndex(
                name: "IX_RequestLogs_StatusCode_Timestamp",
                table: "RequestLogs");

            migrationBuilder.DropIndex(
                name: "IX_RequestLogs_Timestamp",
                table: "RequestLogs");

            migrationBuilder.DropIndex(
                name: "IX_RequestLogs_UserId_Timestamp",
                table: "RequestLogs");

            migrationBuilder.DropIndex(
                name: "IX_PasswordHistories_UserId_CreatedAt",
                table: "PasswordHistories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ZipUploadJobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SiteConfigurations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Showcases");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "LinkCaches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ContentDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CollectibleItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Attachments");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ZipUploadJobs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "ZipUploadJobs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentItemName",
                table: "ZipUploadJobs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SiteConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SiteConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "ShowcaseShareTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ShowcaseShareTokens",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Scheme",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResponseContentType",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestId",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QueryString",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Host",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExceptionType",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "RequestLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PasswordHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "PasswordHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<long>(
                name: "QRCodeId",
                table: "CollectibleItems",
                type: "bigint",
                nullable: true);
        }
    }
}
