using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionNotificationFanoutTypesPhase13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications");

            migrationBuilder.AddCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications",
                sql: "type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'permission.grant_expiring', 'permission.grant_expired', 'group.member_added', 'group.member_removed', 'group.member_expiring', 'group.member_expired', 'share_link.created', 'share_link.revoked', 'email_invite.created', 'email_invite.accepted', 'email_invite.revoked', 'email_invite.delivery_failed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications");

            migrationBuilder.AddCheckConstraint(
                name: "permission_notifications_type_check",
                table: "permission_notifications",
                sql: "type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'permission.grant_expiring', 'permission.grant_expired', 'group.member_added', 'group.member_removed', 'group.member_expiring', 'group.member_expired')");
        }
    }
}
