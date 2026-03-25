namespace Cookbook.Shared.Boards;

public sealed record BoardSummary(Guid Id, string Name, string OwnerUserId, DateTimeOffset CreatedAt);
