using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionNotificationPreferencesPhase12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permission_notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    watched = table.Column<bool>(type: "boolean", nullable: false),
                    muted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_notification_preferences", x => x.id);
                    table.CheckConstraint("permission_notification_preferences_resource_type_check", "resource_type IS NULL OR resource_type IN ('collection', 'document')");
                    table.CheckConstraint("permission_notification_preferences_scope_check", "(resource_type IS NULL AND resource_id IS NULL) OR (resource_type IS NOT NULL AND resource_id IS NOT NULL)");
                    table.CheckConstraint("permission_notification_preferences_watch_mute_check", "NOT (watched AND muted)");
                    table.ForeignKey(
                        name: "FK_permission_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_permission_notification_preferences_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_permission_notification_preferences_user_workspace",
                table: "permission_notification_preferences",
                columns: new[] { "user_id", "workspace_id" });

            migrationBuilder.CreateIndex(
                name: "permission_notification_preferences_resource_user_key",
                table: "permission_notification_preferences",
                columns: new[] { "workspace_id", "user_id", "resource_type", "resource_id" },
                unique: true,
                filter: "resource_type IS NOT NULL AND resource_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "permission_notification_preferences_workspace_user_key",
                table: "permission_notification_preferences",
                columns: new[] { "workspace_id", "user_id" },
                unique: true,
                filter: "resource_type IS NULL AND resource_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission_notification_preferences");
        }
    }
}
