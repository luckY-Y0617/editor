using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeContextActivitySearchPhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_activity_events_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_activity_events_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_url = table.Column<string>(type: "text", nullable: true),
                    link_type = table.Column<string>(type: "text", nullable: false, defaultValue: "reference"),
                    anchor_text = table.Column<string>(type: "text", nullable: true),
                    source_anchor = table.Column<string>(type: "jsonb", nullable: true),
                    target_anchor = table.Column<string>(type: "jsonb", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_links", x => x.id);
                    table.CheckConstraint("document_links_link_type_check", "link_type IN ('reference', 'related', 'embed', 'external')");
                    table.CheckConstraint("document_links_target_check", "target_document_id IS NOT NULL OR target_url IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_document_links_documents_source_document_id",
                        column: x => x.source_document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_links_documents_target_document_id",
                        column: x => x.target_document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_links_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_links_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_search_index",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    text_content = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_search_index", x => x.document_id);
                    table.ForeignKey(
                        name: "FK_document_search_index_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_search_index_spaces_space_id",
                        column: x => x.space_id,
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_search_index_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    version_type = table.Column<string>(type: "text", nullable: false, defaultValue: "system"),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    text_content = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    outline = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    word_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.id);
                    table.CheckConstraint("document_versions_version_no_check", "version_no > 0");
                    table.CheckConstraint("document_versions_version_type_check", "version_type IN ('manual', 'published', 'imported', 'system')");
                    table.CheckConstraint("document_versions_word_count_check", "word_count >= 0");
                    table.ForeignKey(
                        name: "FK_document_versions_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_versions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_versions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "activity_events_actor_idx",
                table: "activity_events",
                columns: new[] { "workspace_id", "actor_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "activity_events_entity_idx",
                table: "activity_events",
                columns: new[] { "workspace_id", "entity_type", "entity_id", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_activity_events_actor_id",
                table: "activity_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "document_links_source_idx",
                table: "document_links",
                columns: new[] { "workspace_id", "source_document_id", "link_type" });

            migrationBuilder.CreateIndex(
                name: "document_links_target_idx",
                table: "document_links",
                columns: new[] { "workspace_id", "target_document_id" },
                filter: "target_document_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_document_links_created_by",
                table: "document_links",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_document_links_source_document_id",
                table: "document_links",
                column: "source_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_links_target_document_id",
                table: "document_links",
                column: "target_document_id");

            migrationBuilder.CreateIndex(
                name: "document_search_workspace_idx",
                table: "document_search_index",
                columns: new[] { "workspace_id", "space_id" });

            migrationBuilder.CreateIndex(
                name: "IX_document_search_index_space_id",
                table: "document_search_index",
                column: "space_id");

            migrationBuilder.CreateIndex(
                name: "document_versions_doc_idx",
                table: "document_versions",
                columns: new[] { "workspace_id", "document_id", "version_no" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "document_versions_document_label_key",
                table: "document_versions",
                columns: new[] { "document_id", "label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "document_versions_document_version_no_key",
                table: "document_versions",
                columns: new[] { "document_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_created_by",
                table: "document_versions",
                column: "created_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_events");

            migrationBuilder.DropTable(
                name: "document_links");

            migrationBuilder.DropTable(
                name: "document_search_index");

            migrationBuilder.DropTable(
                name: "document_versions");
        }
    }
}
