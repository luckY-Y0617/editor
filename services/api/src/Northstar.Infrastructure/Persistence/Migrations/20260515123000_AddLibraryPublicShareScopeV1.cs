using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(NorthstarDbContext))]
    [Migration("20260515123000_AddLibraryPublicShareScopeV1")]
    public partial class AddLibraryPublicShareScopeV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "resource_access_policies_resource_type_check",
                table: "resource_access_policies");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_resource_type_check",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_link_access_events_resource_type_check",
                table: "share_link_access_events");

            migrationBuilder.AddCheckConstraint(
                name: "resource_access_policies_resource_type_check",
                table: "resource_access_policies",
                sql: "resource_type IN ('library', 'collection', 'document')");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links",
                sql: "audience <> 'public' OR (resource_type IN ('document', 'collection', 'library') AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_resource_type_check",
                table: "share_links",
                sql: "resource_type IN ('library', 'collection', 'document')");

            migrationBuilder.AddCheckConstraint(
                name: "share_link_access_events_resource_type_check",
                table: "share_link_access_events",
                sql: "resource_type IN ('library', 'collection', 'document')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "resource_access_policies_resource_type_check",
                table: "resource_access_policies");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_resource_type_check",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_link_access_events_resource_type_check",
                table: "share_link_access_events");

            migrationBuilder.AddCheckConstraint(
                name: "resource_access_policies_resource_type_check",
                table: "resource_access_policies",
                sql: "resource_type IN ('collection', 'document')");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links",
                sql: "audience <> 'public' OR (resource_type IN ('document', 'collection') AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_resource_type_check",
                table: "share_links",
                sql: "resource_type IN ('collection', 'document')");

            migrationBuilder.AddCheckConstraint(
                name: "share_link_access_events_resource_type_check",
                table: "share_link_access_events",
                sql: "resource_type IN ('collection', 'document')");
        }
    }
}
