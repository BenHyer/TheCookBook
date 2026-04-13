using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CookbookMauiBlazor.Shared.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly HttpClient _httpClient;

    public UserProfileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/users/{userId}/profile", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(string userId, string firstName, string lastName, string displayName, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { firstName, lastName, displayName };
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/users/{userId}/profile", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> UploadProfilePictureAsync(string userId, Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"/api/v1/users/{userId}/profile/picture", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<UploadResult>(cancellationToken: cancellationToken);
            return result?.Url;
        }
        catch
        {
            return null;
        }
    }

    private record UploadResult(string Url);
}
