using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectibles.Infrastructure.Migrations
{
    /// <summary>
    /// Swaps the physical Taxonomy tables so each one holds the data its name describes.
    /// </summary>
    /// <remarks>
    /// The two EF configuration classes had been written into each other's files, so
    /// <c>TaxonomyVocabulary</c> mapped to the <c>TaxonomyTerms</c> table and
    /// <c>TaxonomyTerm</c> mapped to <c>TaxonomyVocabularies</c>. EF was internally
    /// consistent, so the application behaved correctly, but every raw SQL query, report,
    /// or DBA operation read the wrong table.
    ///
    /// This is deliberately a rename-through-a-temporary-name in both directions rather
    /// than the drop/add columns EF scaffolds for this change: the rows are already
    /// correct, only the table they sit in is misnamed, and dropping the columns would
    /// destroy the data. Kept as an isolated migration so it can be reviewed on its own.
    /// </remarks>
    public partial class AuditFixInvertedTaxonomyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SwapTaxonomyTables(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The swap is its own inverse.
            SwapTaxonomyTables(migrationBuilder);
        }

        private static void SwapTaxonomyTables(MigrationBuilder migrationBuilder)
        {
            // Constraint and index names travel with their table under sp_rename, so they
            // are renamed explicitly afterwards to keep them consistent with the model.
            migrationBuilder.Sql(@"
                EXEC sp_rename N'TaxonomyTerms', N'TaxonomyTables_Swap';
                EXEC sp_rename N'TaxonomyVocabularies', N'TaxonomyTerms';
                EXEC sp_rename N'TaxonomyTables_Swap', N'TaxonomyVocabularies';");

            // Primary keys.
            migrationBuilder.Sql(@"
                EXEC sp_rename N'PK_TaxonomyTerms', N'PK_TaxonomyTables_Swap', N'OBJECT';
                EXEC sp_rename N'PK_TaxonomyVocabularies', N'PK_TaxonomyTerms', N'OBJECT';
                EXEC sp_rename N'PK_TaxonomyTables_Swap', N'PK_TaxonomyVocabularies', N'OBJECT';");

            // Indexes on the term columns move with the rows, so they now sit on the table
            // called TaxonomyTerms and must be named for it.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaxonomyVocabularies_ParentId' AND object_id = OBJECT_ID(N'TaxonomyTerms'))
                    EXEC sp_rename N'TaxonomyTerms.IX_TaxonomyVocabularies_ParentId', N'IX_TaxonomyTerms_ParentId', N'INDEX';
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaxonomyVocabularies_VocabularyId' AND object_id = OBJECT_ID(N'TaxonomyTerms'))
                    EXEC sp_rename N'TaxonomyTerms.IX_TaxonomyVocabularies_VocabularyId', N'IX_TaxonomyTerms_VocabularyId', N'INDEX';
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaxonomyTerms_ParentId' AND object_id = OBJECT_ID(N'TaxonomyVocabularies'))
                    EXEC sp_rename N'TaxonomyVocabularies.IX_TaxonomyTerms_ParentId', N'IX_TaxonomyVocabularies_ParentId', N'INDEX';
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaxonomyTerms_VocabularyId' AND object_id = OBJECT_ID(N'TaxonomyVocabularies'))
                    EXEC sp_rename N'TaxonomyVocabularies.IX_TaxonomyTerms_VocabularyId', N'IX_TaxonomyVocabularies_VocabularyId', N'INDEX';");

            // Foreign keys.
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'FK_TaxonomyVocabularies_TaxonomyVocabularies_ParentId', N'F') IS NOT NULL
                    EXEC sp_rename N'FK_TaxonomyVocabularies_TaxonomyVocabularies_ParentId', N'FK_Taxonomy_Swap_ParentId', N'OBJECT';
                IF OBJECT_ID(N'FK_TaxonomyVocabularies_TaxonomyTerms_VocabularyId', N'F') IS NOT NULL
                    EXEC sp_rename N'FK_TaxonomyVocabularies_TaxonomyTerms_VocabularyId', N'FK_Taxonomy_Swap_VocabularyId', N'OBJECT';
                IF OBJECT_ID(N'FK_TaxonomyTerms_TaxonomyTerms_ParentId', N'F') IS NOT NULL
                    EXEC sp_rename N'FK_TaxonomyTerms_TaxonomyTerms_ParentId', N'FK_TaxonomyVocabularies_TaxonomyVocabularies_ParentId', N'OBJECT';
                IF OBJECT_ID(N'FK_TaxonomyTerms_TaxonomyVocabularies_VocabularyId', N'F') IS NOT NULL
                    EXEC sp_rename N'FK_TaxonomyTerms_TaxonomyVocabularies_VocabularyId', N'FK_TaxonomyVocabularies_TaxonomyTerms_VocabularyId', N'OBJECT';
                IF OBJECT_ID(N'FK_Taxonomy_Swap_ParentId', N'F') IS NOT NULL
                    EXEC sp_rename N'FK_Taxonomy_Swap_ParentId', N'FK_TaxonomyTerms_TaxonomyTerms_ParentId', N'OBJECT';
                IF OBJECT_ID(N'FK_Taxonomy_Swap_VocabularyId', N'F') IS NOT NULL
                    EXEC sp_rename N'FK_Taxonomy_Swap_VocabularyId', N'FK_TaxonomyTerms_TaxonomyVocabularies_VocabularyId', N'OBJECT';");
        }
    }
}
