using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionExpiryNotificationsPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications");

            migrationBuilder.AddColumn<string>(
                name: "dedupe_key",
                table: "permission_notifications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "permission_notifications_dedupe_key",
                table: "permission_notifications",
                column: "dedupe_key",
                unique: true,
                filter: "dedupe_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications",
                sql: "type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'permission.grant_expiring', 'permission.grant_expired', 'group.member_added', 'group.member_removed', 'group.member_expiring', 'group.member_expired')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "permission_notifications_dedupe_key",
                table: "permission_notifications");

            migrationBuilder.DropCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications");

            migrationBuilder.DropColumn(
                name: "dedupe_key",
                table: "permission_notifications");

            migrationBuilder.AddCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications",
                sql: "type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'group.member_added', 'group.member_removed')");
        }
    }
}
