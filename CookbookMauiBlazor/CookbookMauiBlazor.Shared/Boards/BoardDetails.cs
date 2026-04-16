namespace Cookbook.Shared.Boards;

public sealed record BoardDetails(
    Guid Id,
    string Name,
    string? Description,
    string OwnerUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RecipeCount);
