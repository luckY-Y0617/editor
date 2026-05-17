using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(NorthstarDbContext))]
    [Migration("20260516110000_AllowLibraryPermissionNotifications")]
    public partial class AllowLibraryPermissionNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "permission_notifications_resource_type_check",
                table: "permission_notifications");

            migrationBuilder.AddCheckConstraint(
                name: "permission_notifications_resource_type_check",
                table: "permission_notifications",
                sql: "resource_type IS NULL OR resource_type IN ('workspace', 'library', 'collection', 'document')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "permission_notifications_resource_type_check",
                table: "permission_notifications");

            migrationBuilder.AddCheckConstraint(
                name: "permission_notifications_resource_type_check",
                table: "permission_notifications",
                sql: "resource_type IS NULL OR resource_type IN ('workspace', 'collection', 'document')");
        }
    }
}
