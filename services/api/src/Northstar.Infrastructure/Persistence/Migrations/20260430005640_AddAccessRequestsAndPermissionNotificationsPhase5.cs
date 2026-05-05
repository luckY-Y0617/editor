using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessRequestsAndPermissionNotificationsPhase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "text", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_role = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_reason = table.Column<string>(type: "text", nullable: true),
                    resulting_grant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_requests", x => x.id);
                    table.CheckConstraint("access_requests_requested_role_check", "requested_role IN ('owner', 'admin', 'editor', 'commenter', 'viewer')");
                    table.CheckConstraint("access_requests_resource_type_check", "resource_type IN ('collection', 'document')");
                    table.CheckConstraint("access_requests_status_check", "status IN ('pending', 'approved', 'denied', 'cancelled')");
                    table.CheckConstraint("access_requests_subject_type_check", "subject_type IN ('user', 'group')");
                    table.ForeignKey(
                        name: "FK_access_requests_resource_access_grants_resulting_grant_id",
                        column: x => x.resulting_grant_id,
                        principalTable: "resource_access_grants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_access_requests_users_decided_by",
                        column: x => x.decided_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_access_requests_users_requester_id",
                        column: x => x.requester_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_access_requests_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permission_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    access_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    permission_grant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    action_url = table.Column<string>(type: "text", nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_notifications", x => x.id);
                    table.CheckConstraint("permission_notifications_resource_type_check", "resource_type IS NULL OR resource_type IN ('workspace', 'collection', 'document')");
                    table.CheckConstraint("permission_notifications_type_check", "type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'group.member_added', 'group.member_removed')");
                    table.ForeignKey(
                        name: "FK_permission_notifications_access_requests_access_request_id",
                        column: x => x.access_request_id,
                        principalTable: "access_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_permission_notifications_resource_access_grants_permission_~",
                        column: x => x.permission_grant_id,
                        principalTable: "resource_access_grants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_permission_notifications_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_permission_notifications_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_permission_notifications_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "access_requests_pending_subject_key",
                table: "access_requests",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "idx_access_requests_requester_status",
                table: "access_requests",
                columns: new[] { "requester_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_access_requests_resource_status",
                table: "access_requests",
                columns: new[] { "resource_type", "resource_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_access_requests_workspace_status_created",
                table: "access_requests",
                columns: new[] { "workspace_id", "status", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_access_requests_decided_by",
                table: "access_requests",
                column: "decided_by");

            migrationBuilder.CreateIndex(
                name: "IX_access_requests_resulting_grant_id",
                table: "access_requests",
                column: "resulting_grant_id");

            migrationBuilder.CreateIndex(
                name: "idx_permission_notifications_access_request",
                table: "permission_notifications",
                column: "access_request_id");

            migrationBuilder.CreateIndex(
                name: "idx_permission_notifications_recipient_read_created",
                table: "permission_notifications",
                columns: new[] { "recipient_user_id", "read_at", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_permission_notifications_workspace_created",
                table: "permission_notifications",
                columns: new[] { "workspace_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_permission_notifications_actor_user_id",
                table: "permission_notifications",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_permission_notifications_permission_grant_id",
                table: "permission_notifications",
                column: "permission_grant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission_notifications");

            migrationBuilder.DropTable(
                name: "access_requests");
        }
    }
}
