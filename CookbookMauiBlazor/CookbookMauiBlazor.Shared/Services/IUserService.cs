namespace CookbookMauiBlazor.Shared.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserSummary>> SearchAsync(string? searchQuery = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardCollaborator>> GetBoardCollaboratorsAsync(Guid boardId, CancellationToken cancellationToken = default);
    Task ShareBoardAsync(Guid boardId, IReadOnlyCollection<string> userIds, string callerUserId, string role = "Viewer", CancellationToken cancellationToken = default);
    Task RemovePermissionAsync(Guid boardId, string userId, CancellationToken cancellationToken = default);
}

public record UserSummary(string UserId, string DisplayName, string FirstName, string LastName, string? ProfilePictureUrl);

public record BoardCollaborator(string UserId, string DisplayName, string FirstName, string LastName, string? ProfilePictureUrl, string Role);
