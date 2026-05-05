using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIamSyncPhase8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "users_external_provider_subject_key",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "external_subject",
                table: "users",
                newName: "external_subject_id");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "external_synced_at",
                table: "workspace_groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "workspace_groups_workspace_external_key",
                table: "workspace_groups",
                columns: new[] { "workspace_id", "external_provider", "external_group_id" },
                unique: true,
                filter: "external_provider IS NOT NULL AND external_group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "users_external_provider_subject_key",
                table: "users",
                columns: new[] { "external_provider", "external_subject_id" },
                unique: true,
                filter: "external_provider IS NOT NULL AND external_subject_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "workspace_groups_workspace_external_key",
                table: "workspace_groups");

            migrationBuilder.DropIndex(
                name: "users_external_provider_subject_key",
                table: "users");

            migrationBuilder.DropColumn(
                name: "external_synced_at",
                table: "workspace_groups");

            migrationBuilder.RenameColumn(
                name: "external_subject_id",
                table: "users",
                newName: "external_subject");

            migrationBuilder.CreateIndex(
                name: "users_external_provider_subject_key",
                table: "users",
                columns: new[] { "external_provider", "external_subject" },
                unique: true);
        }
    }
}
