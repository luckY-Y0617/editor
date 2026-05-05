using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalShareLinksAndEmailInvitesPhase9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "share_links_audience_check",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "resource_access_policies_link_mode_check",
                table: "resource_access_policies");

            migrationBuilder.AddColumn<string>(
                name: "subject_email",
                table: "share_links",
                type: "citext",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "resource_email_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    role_key = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_email_invites", x => x.id);
                    table.CheckConstraint("resource_email_invites_resource_type_check", "resource_type IN ('collection', 'document')");
                    table.CheckConstraint("resource_email_invites_role_key_check", "role_key IN ('viewer', 'commenter')");
                    table.CheckConstraint("resource_email_invites_status_check", "status IN ('pending', 'accepted', 'revoked', 'expired')");
                    table.ForeignKey(
                        name: "FK_resource_email_invites_users_accepted_by",
                        column: x => x.accepted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resource_email_invites_users_invited_by",
                        column: x => x.invited_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resource_email_invites_users_revoked_by",
                        column: x => x.revoked_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_resource_email_invites_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "share_links_audience_check",
                table: "share_links",
                sql: "audience IN ('workspace', 'external', 'public')");

            migrationBuilder.AddCheckConstraint(
                name: "resource_access_policies_link_mode_check",
                table: "resource_access_policies",
                sql: "link_mode IN ('disabled', 'internal', 'external', 'public')");

            migrationBuilder.CreateIndex(
                name: "idx_resource_email_invites_pending_expiry",
                table: "resource_email_invites",
                column: "expires_at",
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "idx_resource_email_invites_resource",
                table: "resource_email_invites",
                columns: new[] { "workspace_id", "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_email_invites_token_hash",
                table: "resource_email_invites",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_email_invites_accepted_by",
                table: "resource_email_invites",
                column: "accepted_by");

            migrationBuilder.CreateIndex(
                name: "IX_resource_email_invites_invited_by",
                table: "resource_email_invites",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "IX_resource_email_invites_revoked_by",
                table: "resource_email_invites",
                column: "revoked_by");

            migrationBuilder.CreateIndex(
                name: "resource_email_invites_pending_resource_email_key",
                table: "resource_email_invites",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "email" },
                unique: true,
                filter: "status = 'pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource_email_invites");

            migrationBuilder.DropCheckConstraint(
                name: "share_links_audience_check",
                table: "share_links");

            migrationBuilder.DropCheckConstraint(
                name: "resource_access_policies_link_mode_check",
                table: "resource_access_policies");

            migrationBuilder.DropColumn(
                name: "subject_email",
                table: "share_links");

            migrationBuilder.AddCheckConstraint(
                name: "share_links_audience_check",
                table: "share_links",
                sql: "audience IN ('workspace')");

            migrationBuilder.AddCheckConstraint(
                name: "resource_access_policies_link_mode_check",
                table: "resource_access_policies",
                sql: "link_mode IN ('disabled', 'internal', 'public')");
        }
    }
}
