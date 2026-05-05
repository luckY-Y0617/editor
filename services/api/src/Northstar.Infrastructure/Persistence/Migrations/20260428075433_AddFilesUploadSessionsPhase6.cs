using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFilesUploadSessionsPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_outbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "text", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    headers = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_outbox_events", x => x.id);
                    table.CheckConstraint("file_outbox_events_retry_count_check", "retry_count >= 0");
                    table.CheckConstraint("file_outbox_events_status_check", "status IN ('pending', 'published', 'failed')");
                    table.ForeignKey(
                        name: "FK_file_outbox_events_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_provider = table.Column<string>(type: "text", nullable: false),
                    bucket = table.Column<string>(type: "text", nullable: false),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    original_filename = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    byte_size = table.Column<long>(type: "bigint", nullable: false),
                    checksum_sha256 = table.Column<string>(type: "text", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    scan_status = table.Column<string>(type: "text", nullable: false, defaultValue: "clean"),
                    processing_status = table.Column<string>(type: "text", nullable: false, defaultValue: "ready"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                    table.CheckConstraint("files_byte_size_check", "byte_size >= 0");
                    table.CheckConstraint("files_processing_status_check", "processing_status IN ('pending', 'ready', 'failed')");
                    table.CheckConstraint("files_scan_status_check", "scan_status IN ('pending', 'clean', 'blocked', 'failed')");
                    table.ForeignKey(
                        name: "FK_files_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_files_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type = table.Column<string>(type: "text", nullable: false, defaultValue: "attachment"),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_attachments", x => x.id);
                    table.CheckConstraint("document_attachments_relation_type_check", "relation_type IN ('attachment', 'inline_image', 'cover')");
                    table.ForeignKey(
                        name: "FK_document_attachments_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_attachments_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_attachments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_attachments_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "upload_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    original_filename = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    byte_size = table.Column<long>(type: "bigint", nullable: false),
                    checksum_sha256 = table.Column<string>(type: "text", nullable: true),
                    biz_type = table.Column<string>(type: "text", nullable: true),
                    storage_provider = table.Column<string>(type: "text", nullable: false),
                    bucket = table.Column<string>(type: "text", nullable: false),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    upload_mode = table.Column<string>(type: "text", nullable: false, defaultValue: "single"),
                    multipart_upload_id = table.Column<string>(type: "text", nullable: true),
                    chunk_size = table.Column<long>(type: "bigint", nullable: true),
                    total_parts = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "initiated"),
                    finalized_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upload_sessions", x => x.id);
                    table.CheckConstraint("upload_sessions_byte_size_check", "byte_size > 0");
                    table.CheckConstraint("upload_sessions_status_check", "status IN ('initiated', 'uploading', 'completed', 'aborted', 'expired', 'failed', 'finalized')");
                    table.CheckConstraint("upload_sessions_upload_mode_check", "upload_mode IN ('single', 'multipart')");
                    table.ForeignKey(
                        name: "FK_upload_sessions_files_finalized_file_id",
                        column: x => x.finalized_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_upload_sessions_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_upload_sessions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "document_attachments_document_file_relation_key",
                table: "document_attachments",
                columns: new[] { "document_id", "file_id", "relation_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "document_attachments_document_idx",
                table: "document_attachments",
                columns: new[] { "workspace_id", "document_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "document_attachments_file_idx",
                table: "document_attachments",
                columns: new[] { "workspace_id", "file_id" });

            migrationBuilder.CreateIndex(
                name: "IX_document_attachments_created_by",
                table: "document_attachments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_document_attachments_file_id",
                table: "document_attachments",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "file_outbox_events_aggregate_idx",
                table: "file_outbox_events",
                columns: new[] { "workspace_id", "aggregate_type", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "file_outbox_events_dispatch_idx",
                table: "file_outbox_events",
                columns: new[] { "status", "next_retry_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "files_storage_object_key",
                table: "files",
                columns: new[] { "storage_provider", "bucket", "object_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "files_workspace_created_idx",
                table: "files",
                columns: new[] { "workspace_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_files_uploaded_by",
                table: "files",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_upload_sessions_finalized_file_id",
                table: "upload_sessions",
                column: "finalized_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_upload_sessions_owner_id",
                table: "upload_sessions",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "upload_sessions_owner_idx",
                table: "upload_sessions",
                columns: new[] { "workspace_id", "owner_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "upload_sessions_status_expires_idx",
                table: "upload_sessions",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "upload_sessions_storage_object_key",
                table: "upload_sessions",
                columns: new[] { "storage_provider", "bucket", "object_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "upload_sessions_workspace_idempotency_key",
                table: "upload_sessions",
                columns: new[] { "workspace_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_attachments");

            migrationBuilder.DropTable(
                name: "file_outbox_events");

            migrationBuilder.DropTable(
                name: "upload_sessions");

            migrationBuilder.DropTable(
                name: "files");
        }
    }
}
