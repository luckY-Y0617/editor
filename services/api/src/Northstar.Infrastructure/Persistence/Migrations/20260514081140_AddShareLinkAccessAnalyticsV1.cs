using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShareLinkAccessAnalyticsV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "share_link_access_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audience = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    result = table.Column<string>(type: "text", nullable: false),
                    failure_category = table.Column<string>(type: "text", nullable: true),
                    remote_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_link_access_events", x => x.id);
                    table.CheckConstraint("share_link_access_events_audience_check", "audience IN ('workspace', 'external', 'public')");
                    table.CheckConstraint("share_link_access_events_event_type_check", "event_type IN ('resolve', 'access', 'download')");
                    table.CheckConstraint("share_link_access_events_resource_type_check", "resource_type IN ('collection', 'document')");
                    table.CheckConstraint("share_link_access_events_result_check", "result IN ('success', 'fail')");
                    table.ForeignKey(
                        name: "FK_share_link_access_events_share_links_share_link_id",
                        column: x => x.share_link_id,
                        principalTable: "share_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_share_link_access_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_share_link_access_events_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_link_access_stats",
                columns: table => new
                {
                    share_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_accessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    access_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    unique_visitor_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    last_access_ip = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_link_access_stats", x => x.share_link_id);
                    table.ForeignKey(
                        name: "FK_share_link_access_stats_share_links_share_link_id",
                        column: x => x.share_link_id,
                        principalTable: "share_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_share_link_access_stats_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_share_link_access_events_link_time",
                table: "share_link_access_events",
                columns: new[] { "workspace_id", "share_link_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_share_link_access_events_resource_time",
                table: "share_link_access_events",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "occurred_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_share_link_access_events_workspace_time",
                table: "share_link_access_events",
                columns: new[] { "workspace_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_share_link_access_events_actor_user_id",
                table: "share_link_access_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_share_link_access_events_share_link_id",
                table: "share_link_access_events",
                column: "share_link_id");

            migrationBuilder.CreateIndex(
                name: "IX_share_link_access_stats_workspace_id",
                table: "share_link_access_stats",
                column: "workspace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "share_link_access_events");

            migrationBuilder.DropTable(
                name: "share_link_access_stats");
        }
    }
}
