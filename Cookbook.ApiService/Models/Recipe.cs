using System.Collections.Generic;

namespace Cookbook.ApiService.Models;

public class Recipe
{
    public int Id { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? YieldServings { get; set; }
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public string? TotalTime { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public List<string> Quantities { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public string? CookingTemperature { get; set; }
    public string? NutritionFacts { get; set; }
    public string? StorageInfo { get; set; }
    public string? ImageUrl { get; set; }
}
