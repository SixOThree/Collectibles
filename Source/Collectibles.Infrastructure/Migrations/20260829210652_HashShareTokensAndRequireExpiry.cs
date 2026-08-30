using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <summary>
    /// Replaces the plaintext share token with a one-way hash, and makes expiry mandatory.
    /// </summary>
    /// <remarks>
    /// Hand-ordered rather than left as scaffolded. The generated version dropped <c>Token</c>
    /// before anything could read it, defaulted every <c>TokenHash</c> to the empty string - which
    /// the new unique index would reject for the second row onwards - and set existing null
    /// expiries to year 0001, silently invalidating every live share link.
    ///
    /// This version adds the column nullable, derives each hash from the token still in place, gives
    /// rows that never had an expiry a forward-dated one so working links keep working for a grace
    /// period, and only then tightens the columns and drops the plaintext.
    ///
    /// The hash must match <c>ShareTokenHash.Compute</c>, which is lowercase hex SHA-256 over the
    /// UTF-8 bytes of the token. Tokens are base64url, so they are pure ASCII and
    /// <c>CONVERT(varchar(...))</c> produces the same bytes UTF-8 would.
    /// </remarks>
    public partial class HashShareTokensAndRequireExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the hash column nullable so existing rows survive the add.
            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "ShowcaseShareTokens",
                type: "nchar(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            // 2. Derive each hash while the plaintext is still available.
            migrationBuilder.Sql(@"
                UPDATE [ShowcaseShareTokens]
                SET [TokenHash] = LOWER(
                    CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varchar(64), [Token])), 2))
                WHERE [Token] IS NOT NULL AND [Token] <> '';");

            // 3. Any row we could not derive a hash for can never be presented successfully.
            //    Retire it rather than leaving an unusable row blocking the unique index.
            migrationBuilder.Sql(@"
                DELETE FROM [ShowcaseShareTokens]
                WHERE [TokenHash] IS NULL;");

            // 4. Collapse any duplicate hashes, which the old application-level uniqueness check
            //    could in principle have let through, keeping the earliest row.
            migrationBuilder.Sql(@"
                WITH Ranked AS (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [TokenHash] ORDER BY [Id]) AS rn
                    FROM [ShowcaseShareTokens]
                )
                DELETE FROM [ShowcaseShareTokens]
                WHERE [Id] IN (SELECT [Id] FROM Ranked WHERE rn > 1);");

            // 5. Give perpetual links a bounded life rather than expiring them on the spot.
            migrationBuilder.Sql(@"
                UPDATE [ShowcaseShareTokens]
                SET [ExpiresAt] = DATEADD(day, 30, SYSUTCDATETIME())
                WHERE [ExpiresAt] IS NULL;");

            // 6. Tighten both columns now that every row has a value.
            migrationBuilder.Sql(@"
                ALTER TABLE [ShowcaseShareTokens]
                ALTER COLUMN [TokenHash] nchar(64) NOT NULL;");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShowcaseShareTokens]
                ALTER COLUMN [ExpiresAt] datetime2 NOT NULL;");

            // 7. Retire the plaintext and index the hash.
            migrationBuilder.DropIndex(
                name: "IX_ShowcaseShareTokens_Token",
                table: "ShowcaseShareTokens");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "ShowcaseShareTokens");

            migrationBuilder.CreateIndex(
                name: "IX_ShowcaseShareTokens_TokenHash",
                table: "ShowcaseShareTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The plaintext tokens are gone and a hash cannot be reversed, so this restores the
            // shape of the old schema but not its contents. Rows are removed rather than
            // resurrected with empty tokens, which the unique index would reject anyway.
            migrationBuilder.DropIndex(
                name: "IX_ShowcaseShareTokens_TokenHash",
                table: "ShowcaseShareTokens");

            migrationBuilder.Sql("DELETE FROM [ShowcaseShareTokens];");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "ShowcaseShareTokens");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShowcaseShareTokens]
                ALTER COLUMN [ExpiresAt] datetime2 NULL;");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "ShowcaseShareTokens",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_ShowcaseShareTokens_Token",
                table: "ShowcaseShareTokens",
                column: "Token",
                unique: true);
        }
    }
}
