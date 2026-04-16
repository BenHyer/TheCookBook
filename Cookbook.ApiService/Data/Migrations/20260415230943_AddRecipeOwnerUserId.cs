using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cookbook.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Recipes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_OwnerUserId",
                table: "Recipes",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_OwnerUserId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Recipes");
        }
    }
}
