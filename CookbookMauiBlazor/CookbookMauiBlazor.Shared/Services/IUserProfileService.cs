namespace CookbookMauiBlazor.Shared.Services;

public record UserProfileDto(string UserId, string FirstName, string LastName, string DisplayName, string? ProfilePictureUrl);

public interface IUserProfileService
{
    Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateProfileAsync(string userId, string firstName, string lastName, string displayName, CancellationToken cancellationToken = default);
    Task<string?> UploadProfilePictureAsync(string userId, Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
