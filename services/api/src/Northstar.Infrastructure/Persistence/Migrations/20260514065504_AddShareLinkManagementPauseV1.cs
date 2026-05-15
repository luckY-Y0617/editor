using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShareLinkManagementPauseV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pause_reason",
                table: "share_links",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paused_at",
                table: "share_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "paused_by",
                table: "share_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_share_links_paused_by",
                table: "share_links",
                column: "paused_by");

            migrationBuilder.AddForeignKey(
                name: "FK_share_links_users_paused_by",
                table: "share_links",
                column: "paused_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_share_links_users_paused_by",
                table: "share_links");

            migrationBuilder.DropIndex(
                name: "IX_share_links_paused_by",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "pause_reason",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "paused_at",
                table: "share_links");

            migrationBuilder.DropColumn(
                name: "paused_by",
                table: "share_links");
        }
    }
}
