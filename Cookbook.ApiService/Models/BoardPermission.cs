namespace Cookbook.ApiService.Models;

public class BoardPermission
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = BoardRoles.Admin;
    public DateTime CreatedUtc { get; set; }

    public Board Board { get; set; } = null!;
}
