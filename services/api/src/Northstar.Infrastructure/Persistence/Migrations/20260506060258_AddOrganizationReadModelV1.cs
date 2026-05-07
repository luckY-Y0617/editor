using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationReadModelV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var defaultOrganizationId = new Guid("09000000-0000-0000-0000-000000000001");

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "id", "name", "slug", "status" },
                values: new object[] { defaultOrganizationId, "Northstar", "northstar", "active" });

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "workspaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"UPDATE workspaces SET organization_id = '{defaultOrganizationId}' WHERE organization_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "workspaces",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_organization_id",
                table: "workspaces",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "organizations_slug_key",
                table: "organizations",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_workspaces_organizations_organization_id",
                table: "workspaces",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workspaces_organizations_organization_id",
                table: "workspaces");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_workspaces_organization_id",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "workspaces");
        }
    }
}
