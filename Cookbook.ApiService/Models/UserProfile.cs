namespace Cookbook.ApiService.Models;

public class UserProfile
{
    /// <summary>Azure AD object ID — same value used as OwnerUserId on boards.</summary>
    public string UserId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Public Azure Blob URL, null until user uploads a picture.</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
