using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMfaMethodsPhase16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_mfa_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_type = table.Column<string>(type: "text", nullable: false),
                    secret_ciphertext = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_mfa_methods", x => x.id);
                    table.CheckConstraint("user_mfa_methods_method_type_check", "method_type IN ('totp')");
                    table.CheckConstraint("user_mfa_methods_status_check", "status IN ('pending', 'enabled', 'disabled')");
                    table.ForeignKey(
                        name: "FK_user_mfa_methods_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_mfa_methods_active_type",
                table: "user_mfa_methods",
                columns: new[] { "user_id", "method_type" },
                unique: true,
                filter: "status IN ('pending', 'enabled')");

            migrationBuilder.CreateIndex(
                name: "idx_user_mfa_methods_user_status",
                table: "user_mfa_methods",
                columns: new[] { "user_id", "status", "created_at" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_mfa_methods");
        }
    }
}
