namespace Cookbook.ApiService.Models;

public class Board
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<BoardPermission> Permissions { get; set; } = new List<BoardPermission>();
}
