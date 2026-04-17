using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cookbook.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardPermissionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BoardPermissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "BoardPermissions");
        }
    }
}
