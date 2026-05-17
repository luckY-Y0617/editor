using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northstar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(NorthstarDbContext))]
    [Migration("20260515120000_AddShareLinkContentProtectionV1")]
    public partial class AddShareLinkContentProtectionV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_protection_json",
                table: "share_links",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_protection_json",
                table: "share_links");
        }
    }
}
