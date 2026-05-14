using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchFullTextAndTrigramStrategyV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pg_trgm;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE document_search_index
                ADD COLUMN IF NOT EXISTS search_vector tsvector
                GENERATED ALWAYS AS (
                    setweight(to_tsvector('simple', coalesce(title, '')), 'A') ||
                    setweight(to_tsvector('simple', coalesce(text_content, '')), 'B')
                ) STORED;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS document_search_vector_idx
                    ON document_search_index
                    USING GIN (search_vector);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS document_search_title_trgm_idx
                    ON document_search_index
                    USING GIN (lower(title) gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS document_search_text_trgm_idx
                    ON document_search_index
                    USING GIN (lower(text_content) gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS document_search_text_trgm_idx;
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS document_search_title_trgm_idx;
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS document_search_vector_idx;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE document_search_index
                DROP COLUMN IF EXISTS search_vector;
                """);
        }
    }
}
