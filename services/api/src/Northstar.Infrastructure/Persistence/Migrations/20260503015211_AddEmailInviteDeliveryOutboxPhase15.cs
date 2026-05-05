using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailInviteDeliveryOutboxPhase15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_invite_delivery_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_email = table.Column<string>(type: "citext", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "text", nullable: true),
                    last_error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_invite_delivery_outbox", x => x.id);
                    table.CheckConstraint("email_invite_delivery_outbox_attempts_check", "attempt_count >= 0 AND max_attempts > 0 AND attempt_count <= max_attempts");
                    table.CheckConstraint("email_invite_delivery_outbox_status_check", "status IN ('pending', 'retry_scheduled', 'sent', 'failed')");
                    table.ForeignKey(
                        name: "FK_email_invite_delivery_outbox_resource_email_invites_invite_~",
                        column: x => x.invite_id,
                        principalTable: "resource_email_invites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_email_invite_delivery_outbox_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_email_invite_delivery_outbox_due",
                table: "email_invite_delivery_outbox",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_email_invite_delivery_outbox_failed",
                table: "email_invite_delivery_outbox",
                columns: new[] { "status", "failed_at" });

            migrationBuilder.CreateIndex(
                name: "idx_email_invite_delivery_outbox_invite",
                table: "email_invite_delivery_outbox",
                columns: new[] { "workspace_id", "invite_id" });

            migrationBuilder.CreateIndex(
                name: "IX_email_invite_delivery_outbox_invite_id",
                table: "email_invite_delivery_outbox",
                column: "invite_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_invite_delivery_outbox");
        }
    }
}
