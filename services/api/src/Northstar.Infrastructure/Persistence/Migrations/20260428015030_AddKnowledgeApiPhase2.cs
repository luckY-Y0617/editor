using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeApiPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "citext", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    external_provider = table.Column<string>(type: "text", nullable: true),
                    external_subject = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_tags_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tags_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_members",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_members", x => new { x.workspace_id, x.user_id });
                    table.CheckConstraint("workspace_members_role_check", "role IN ('owner', 'admin', 'editor', 'viewer')");
                    table.CheckConstraint("workspace_members_status_check", "status IN ('invited', 'active', 'suspended')");
                    table.ForeignKey(
                        name: "FK_workspace_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workspace_members_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_tags",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_tags", x => new { x.document_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_document_tags_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_tags_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_created_by",
                table: "workspaces",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_spaces_created_by",
                table: "spaces",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_documents_created_by",
                table: "documents",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_documents_last_edited_by",
                table: "documents",
                column: "last_edited_by");

            migrationBuilder.CreateIndex(
                name: "IX_documents_owner_id",
                table: "documents",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_drafts_updated_by",
                table: "document_drafts",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_collections_created_by",
                table: "collections",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "document_tags_tag_idx",
                table: "document_tags",
                columns: new[] { "workspace_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "IX_document_tags_tag_id",
                table: "document_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tags_created_by",
                table: "tags",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "tags_workspace_slug_key",
                table: "tags",
                columns: new[] { "workspace_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_external_provider_subject_key",
                table: "users",
                columns: new[] { "external_provider", "external_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_user_id",
                table: "workspace_members",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_collections_users_created_by",
                table: "collections",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_document_drafts_users_updated_by",
                table: "document_drafts",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_created_by",
                table: "documents",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_last_edited_by",
                table: "documents",
                column: "last_edited_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_owner_id",
                table: "documents",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_spaces_users_created_by",
                table: "spaces",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_workspaces_users_created_by",
                table: "workspaces",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_collections_users_created_by",
                table: "collections");

            migrationBuilder.DropForeignKey(
                name: "FK_document_drafts_users_updated_by",
                table: "document_drafts");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_created_by",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_last_edited_by",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_owner_id",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_spaces_users_created_by",
                table: "spaces");

            migrationBuilder.DropForeignKey(
                name: "FK_workspaces_users_created_by",
                table: "workspaces");

            migrationBuilder.DropTable(
                name: "document_tags");

            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "IX_workspaces_created_by",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "IX_spaces_created_by",
                table: "spaces");

            migrationBuilder.DropIndex(
                name: "IX_documents_created_by",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_last_edited_by",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_owner_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_document_drafts_updated_by",
                table: "document_drafts");

            migrationBuilder.DropIndex(
                name: "IX_collections_created_by",
                table: "collections");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");
        }
    }
}
