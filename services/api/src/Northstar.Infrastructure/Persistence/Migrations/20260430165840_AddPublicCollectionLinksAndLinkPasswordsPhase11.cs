using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicCollectionLinksAndLinkPasswordsPhase11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_share_links_public_document_active",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links");

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "share_links",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_share_links_public_active",
                table: "share_links",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "expires_at" },
                filter: "audience = 'public' AND revoked_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_password_public_check",
                table: "share_links",
                sql: "password_hash IS NULL OR audience = 'public'");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links",
                sql: "audience <> 'public' OR (resource_type IN ('document', 'collection') AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_share_links_public_active",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_password_public_check",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "share_links");

            migrationBuilder.CreateIndex(
                name: "idx_share_links_public_document_active",
                table: "share_links",
                columns: new[] { "workspace_id", "resource_id", "expires_at" },
                filter: "audience = 'public' AND revoked_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links",
                sql: "audience <> 'public' OR (resource_type = 'document' AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL)");
        }
    }
}
