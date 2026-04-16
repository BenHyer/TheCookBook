using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cookbook.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardRecipes",
                columns: table => new
                {
                    BoardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardRecipes", x => new { x.BoardId, x.RecipeId });
                    table.ForeignKey(
                        name: "FK_BoardRecipes_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardRecipes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardRecipes_RecipeId",
                table: "BoardRecipes",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardRecipes");
        }
    }
}
