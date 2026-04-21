namespace Cookbook.ApiService.Models;

public class AuditLogEntry
{
    public long Id { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? Details { get; set; }
}
