using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceAccessPoliciesPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resource_access_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "text", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_key = table.Column<string>(type: "text", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_access_grants", x => x.id);
                    table.CheckConstraint("resource_access_grants_resource_type_check", "resource_type IN ('collection', 'document')");
                    table.CheckConstraint("resource_access_grants_role_key_check", "role_key IN ('owner', 'admin', 'editor', 'commenter', 'viewer')");
                    table.CheckConstraint("resource_access_grants_subject_type_check", "subject_type IN ('user')");
                    table.ForeignKey(
                        name: "FK_resource_access_grants_users_granted_by",
                        column: x => x.granted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resource_access_grants_users_revoked_by",
                        column: x => x.revoked_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resource_access_grants_users_subject_id",
                        column: x => x.subject_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource_access_grants_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_access_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inheritance_mode = table.Column<string>(type: "text", nullable: false, defaultValue: "inherit"),
                    link_mode = table.Column<string>(type: "text", nullable: false, defaultValue: "disabled"),
                    default_link_role = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_access_policies", x => x.id);
                    table.CheckConstraint("resource_access_policies_default_link_role_check", "default_link_role IS NULL OR default_link_role IN ('viewer', 'commenter')");
                    table.CheckConstraint("resource_access_policies_inheritance_mode_check", "inheritance_mode IN ('inherit', 'restricted')");
                    table.CheckConstraint("resource_access_policies_link_mode_check", "link_mode IN ('disabled', 'internal', 'public')");
                    table.CheckConstraint("resource_access_policies_resource_type_check", "resource_type IN ('collection', 'document')");
                    table.ForeignKey(
                        name: "FK_resource_access_policies_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resource_access_policies_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_grants_expiry",
                table: "resource_access_grants",
                column: "expires_at",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_grants_subject",
                table: "resource_access_grants",
                columns: new[] { "workspace_id", "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "idx_grants_workspace_resource",
                table: "resource_access_grants",
                columns: new[] { "workspace_id", "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_access_grants_granted_by",
                table: "resource_access_grants",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_resource_access_grants_revoked_by",
                table: "resource_access_grants",
                column: "revoked_by");

            migrationBuilder.CreateIndex(
                name: "IX_resource_access_grants_subject_id",
                table: "resource_access_grants",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "resource_access_grants_resource_subject_key",
                table: "resource_access_grants",
                columns: new[] { "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_policies_resource",
                table: "resource_access_policies",
                columns: new[] { "resource_type", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_access_policies_created_by",
                table: "resource_access_policies",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_resource_access_policies_workspace_id",
                table: "resource_access_policies",
                column: "workspace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource_access_grants");

            migrationBuilder.DropTable(
                name: "resource_access_policies");
        }
    }
}
