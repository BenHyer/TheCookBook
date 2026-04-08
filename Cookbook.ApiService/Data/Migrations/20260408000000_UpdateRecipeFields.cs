using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cookbook.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecipeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Recipes",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "CookTime",
                table: "Recipes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CookingTemperature",
                table: "Recipes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Ingredients",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "NutritionFacts",
                table: "Recipes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrepTime",
                table: "Recipes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Quantities",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "StorageInfo",
                table: "Recipes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotalTime",
                table: "Recipes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YieldServings",
                table: "Recipes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CookTime",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CookingTemperature",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Equipment",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Ingredients",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "NutritionFacts",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "PrepTime",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Quantities",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "StorageInfo",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TotalTime",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "YieldServings",
                table: "Recipes");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Recipes",
                newName: "Name");
        }
    }
}
