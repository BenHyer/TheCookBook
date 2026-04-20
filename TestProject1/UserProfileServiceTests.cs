using System.Net;
using System.Net.Http.Json;
using CookbookMauiBlazor.Shared.Services;
using RichardSzalay.MockHttp;

namespace TestProject1;

public class UserProfileServiceTests
{
    private static (UserProfileService service, MockHttpMessageHandler handler) Create()
    {
        var handler = new MockHttpMessageHandler();
        var client = handler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");
        return (new UserProfileService(client), handler);
    }

    private static UserProfileDto MakeProfile(string userId = "user1") =>
        new(userId, "John", "Doe", "JD", "https://cdn.example.com/pic.jpg");

    // --- GetProfileAsync ---

    [Fact]
    public async Task GetProfileAsync_OnSuccess_ReturnsProfile()
    {
        var (svc, handler) = Create();
        var profile = MakeProfile();
        handler.When("/api/v1/users/user1/profile").Respond(HttpStatusCode.OK, JsonContent.Create(profile));

        var result = await svc.GetProfileAsync("user1");

        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("https://cdn.example.com/pic.jpg", result.ProfilePictureUrl);
    }

    [Fact]
    public async Task GetProfileAsync_OnNotFound_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/users/user1/profile").Respond(HttpStatusCode.NotFound);

        var result = await svc.GetProfileAsync("user1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileAsync_OnServerError_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/users/user1/profile").Respond(HttpStatusCode.InternalServerError);

        var result = await svc.GetProfileAsync("user1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileAsync_OnException_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/users/user1/profile").Throw(new HttpRequestException());

        var result = await svc.GetProfileAsync("user1");

        Assert.Null(result);
    }

    // --- UpdateProfileAsync ---

    [Fact]
    public async Task UpdateProfileAsync_OnSuccess_ReturnsUpdatedProfile()
    {
        var (svc, handler) = Create();
        var updated = new UserProfileDto("user1", "Jane", "Smith", "JS", null);
        handler.When(HttpMethod.Put, "/api/v1/users/user1/profile")
            .Respond(HttpStatusCode.OK, JsonContent.Create(updated));

        var result = await svc.UpdateProfileAsync("user1", "Jane", "Smith", "JS");

        Assert.NotNull(result);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("JS", result.DisplayName);
    }

    [Fact]
    public async Task UpdateProfileAsync_OnHttpError_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When(HttpMethod.Put, "/api/v1/users/user1/profile").Respond(HttpStatusCode.BadRequest);

        var result = await svc.UpdateProfileAsync("user1", "Jane", "Smith", "JS");

        Assert.Null(result);
    }

    // --- UploadProfilePictureAsync ---

    [Fact]
    public async Task UploadProfilePictureAsync_OnSuccess_ReturnsUrl()
    {
        var (svc, handler) = Create();
        handler.When(HttpMethod.Post, "/api/v1/users/user1/profile/picture")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new { url = "https://cdn.example.com/new.jpg" }));

        var result = await svc.UploadProfilePictureAsync("user1", new MemoryStream([1, 2, 3]), "pic.jpg", "image/jpeg");

        Assert.Equal("https://cdn.example.com/new.jpg", result);
    }

    [Fact]
    public async Task UploadProfilePictureAsync_OnHttpError_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When(HttpMethod.Post, "/api/v1/users/user1/profile/picture")
            .Respond(HttpStatusCode.InternalServerError);

        var result = await svc.UploadProfilePictureAsync("user1", new MemoryStream([1, 2, 3]), "pic.jpg", "image/jpeg");

        Assert.Null(result);
    }
}
