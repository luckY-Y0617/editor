using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceGroupsPhase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resource_access_grants_users_subject_id",
                table: "resource_access_grants");

            migrationBuilder.DropIndex(
                name: "IX_resource_access_grants_subject_id",
                table: "resource_access_grants");

            migrationBuilder.DropIndex(
                name: "resource_access_grants_resource_subject_key",
                table: "resource_access_grants");

            migrationBuilder.DropCheckConstraint(
                name: "resource_access_grants_subject_type_check",
                table: "resource_access_grants");

            migrationBuilder.CreateTable(
                name: "workspace_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    external_provider = table.Column<string>(type: "text", nullable: true),
                    external_group_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_groups", x => x.id);
                    table.CheckConstraint("workspace_groups_type_check", "type IN ('static', 'dynamic')");
                    table.ForeignKey(
                        name: "FK_workspace_groups_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workspace_groups_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_group_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: true),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_group_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspace_group_members_users_added_by",
                        column: x => x.added_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workspace_group_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workspace_group_members_workspace_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "workspace_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_grants_workspace_resource_subject_type",
                table: "resource_access_grants",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "subject_type" });

            migrationBuilder.CreateIndex(
                name: "resource_access_grants_resource_subject_key",
                table: "resource_access_grants",
                columns: new[] { "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "resource_access_grants_subject_type_check",
                table: "resource_access_grants",
                sql: "subject_type IN ('user', 'group')");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_group_members_user_active",
                table: "workspace_group_members",
                columns: new[] { "user_id", "removed_at", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_group_members_added_by",
                table: "workspace_group_members",
                column: "added_by");

            migrationBuilder.CreateIndex(
                name: "workspace_group_members_group_user_active_key",
                table: "workspace_group_members",
                columns: new[] { "group_id", "user_id" },
                unique: true,
                filter: "removed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_workspace_groups_workspace_archived",
                table: "workspace_groups",
                columns: new[] { "workspace_id", "archived_at" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_groups_created_by",
                table: "workspace_groups",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "workspace_groups_workspace_name_active_key",
                table: "workspace_groups",
                columns: new[] { "workspace_id", "name" },
                unique: true,
                filter: "archived_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workspace_group_members");

            migrationBuilder.DropTable(
                name: "workspace_groups");

            migrationBuilder.DropIndex(
                name: "idx_grants_workspace_resource_subject_type",
                table: "resource_access_grants");

            migrationBuilder.DropIndex(
                name: "resource_access_grants_resource_subject_key",
                table: "resource_access_grants");

            migrationBuilder.DropCheckConstraint(
                name: "resource_access_grants_subject_type_check",
                table: "resource_access_grants");

            migrationBuilder.CreateIndex(
                name: "IX_resource_access_grants_subject_id",
                table: "resource_access_grants",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "resource_access_grants_resource_subject_key",
                table: "resource_access_grants",
                columns: new[] { "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "resource_access_grants_subject_type_check",
                table: "resource_access_grants",
                sql: "subject_type IN ('user')");

            migrationBuilder.AddForeignKey(
                name: "FK_resource_access_grants_users_subject_id",
                table: "resource_access_grants",
                column: "subject_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
