using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicShareLinksAndInviteDeliveryPhase10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivery_attempted_at",
                table: "resource_email_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_error_code",
                table: "resource_email_invites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_provider",
                table: "resource_email_invites",
                type: "text",
                nullable: false,
                defaultValue: "noop");

            migrationBuilder.AddColumn<string>(
                name: "delivery_status",
                table: "resource_email_invites",
                type: "text",
                nullable: false,
                defaultValue: "disabled");

            migrationBuilder.CreateIndex(
                name: "idx_share_links_public_document_active",
                table: "share_links",
                columns: new[] { "workspace_id", "resource_id", "expires_at" },
                filter: "audience = 'public' AND revoked_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links",
                sql: "audience <> 'public' OR (resource_type = 'document' AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_resource_email_invites_delivery_status_created",
                table: "resource_email_invites",
                columns: new[] { "delivery_status", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "resource_email_invites_delivery_status_check",
                table: "resource_email_invites",
                sql: "delivery_status IN ('disabled', 'sent', 'failed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_share_links_public_document_active",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_public_viewer_expiry_check",
                table: "share_links");

            migrationBuilder.DropIndex(
                name: "idx_resource_email_invites_delivery_status_created",
                table: "resource_email_invites");

            migrationBuilder.DropCheckConstraint(
                name: "resource_email_invites_delivery_status_check",
                table: "resource_email_invites");

            migrationBuilder.DropColumn(
                name: "delivery_attempted_at",
                table: "resource_email_invites");

            migrationBuilder.DropColumn(
                name: "delivery_error_code",
                table: "resource_email_invites");

            migrationBuilder.DropColumn(
                name: "delivery_provider",
                table: "resource_email_invites");

            migrationBuilder.DropColumn(
                name: "delivery_status",
                table: "resource_email_invites");
        }
    }
}
