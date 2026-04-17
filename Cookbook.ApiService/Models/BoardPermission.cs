namespace Cookbook.ApiService.Models;

public class BoardPermission
{
    public int Id { get; set; }
    public Guid BoardId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = BoardRoles.Admin;
    public string Status { get; set; } = BoardPermissionStatus.Active;
    public DateTime CreatedUtc { get; set; }

    public Board Board { get; set; } = null!;
}

public static class BoardPermissionStatus
{
    public const string Active = "Active";
    public const string Pending = "Pending";
}
