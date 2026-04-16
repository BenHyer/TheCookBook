namespace Cookbook.Shared.Boards;

public sealed record BoardRecipeSummary(
    int Id,
    string Title,
    string? Description,
    string? ImageUrl);
