namespace Cookbook.ApiService.Models;

public class BoardRecipe
{
    public Guid BoardId { get; set; }
    public int RecipeId { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Board Board { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
