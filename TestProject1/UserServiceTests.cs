using System.Net;
using System.Net.Http.Json;
using CookbookMauiBlazor.Shared.Services;
using RichardSzalay.MockHttp;

namespace TestProject1;

public class UserServiceTests
{
    private static (UserService service, MockHttpMessageHandler handler) Create()
    {
        var handler = new MockHttpMessageHandler();
        var client = handler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");
        return (new UserService(client), handler);
    }

    // --- SearchAsync ---

    [Fact]
    public async Task SearchAsync_WithNoQuery_CallsBaseEndpoint()
    {
        var (svc, handler) = Create();
        var users = new List<UserSummary> { new("u1", "Alice", "Alice", "Smith", null) };
        handler.When("/api/v1/users").Respond(HttpStatusCode.OK, JsonContent.Create(users));

        var result = await svc.SearchAsync();

        Assert.Single(result);
        Assert.Equal("Alice", result[0].DisplayName);
    }

    [Fact]
    public async Task SearchAsync_WithNullQuery_CallsBaseEndpoint()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/users").Respond(HttpStatusCode.OK, JsonContent.Create(new List<UserSummary>()));

        var result = await svc.SearchAsync(null);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_AppendsSearchParam()
    {
        var (svc, handler) = Create();
        var users = new List<UserSummary> { new("u2", "Bob", "Bob", "Jones", null) };
        handler.When("/api/v1/users?search=bob").Respond(HttpStatusCode.OK, JsonContent.Create(users));

        var result = await svc.SearchAsync("bob");

        Assert.Single(result);
        Assert.Equal("Bob", result[0].DisplayName);
    }

    [Fact]
    public async Task SearchAsync_EscapesSpecialCharactersInQuery()
    {
        var (svc, handler) = Create();
        var escaped = Uri.EscapeDataString("alice & bob");
        handler.When($"/api/v1/users?search={escaped}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<UserSummary>()));

        var result = await svc.SearchAsync("alice & bob");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchAsync_OnException_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/users").Throw(new HttpRequestException());

        var result = await svc.SearchAsync();

        Assert.Empty(result);
    }

    // --- GetBoardCollaboratorsAsync ---

    [Fact]
    public async Task GetBoardCollaboratorsAsync_OnSuccess_ReturnsCollaborators()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        var collaborators = new List<BoardCollaborator>
        {
            new("u1", "Alice", "Alice", "Smith", null, "Admin"),
            new("u2", "Bob", "Bob", "Jones", null, "Viewer"),
        };
        handler.When($"/api/v1/boards/{boardId}/collaborators")
            .Respond(HttpStatusCode.OK, JsonContent.Create(collaborators));

        var result = await svc.GetBoardCollaboratorsAsync(boardId);

        Assert.Equal(2, result.Count);
        Assert.Equal("Admin", result[0].Role);
    }

    [Fact]
    public async Task GetBoardCollaboratorsAsync_OnException_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        handler.When($"/api/v1/boards/{boardId}/collaborators").Throw(new HttpRequestException());

        var result = await svc.GetBoardCollaboratorsAsync(boardId);

        Assert.Empty(result);
    }

    // --- ShareBoardAsync ---

    [Fact]
    public async Task ShareBoardAsync_WithEmptyUserIds_DoesNotCallApi()
    {
        var (svc, handler) = Create();
        handler.Fallback.Throw(new Exception("Should not call API"));

        await svc.ShareBoardAsync(Guid.NewGuid(), []);
    }

    [Fact]
    public async Task ShareBoardAsync_CallsCorrectEndpoint()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        var request = handler.Expect(HttpMethod.Post, $"/api/v1/boards/{boardId}/share")
            .Respond(HttpStatusCode.OK);

        await svc.ShareBoardAsync(boardId, ["u1", "u2"]);

        handler.VerifyNoOutstandingExpectation();
    }

    // --- RemovePermissionAsync ---

    [Fact]
    public async Task RemovePermissionAsync_WithBlankUserId_DoesNotCallApi()
    {
        var (svc, handler) = Create();
        handler.Fallback.Throw(new Exception("Should not call API"));

        await svc.RemovePermissionAsync(Guid.NewGuid(), "   ");
    }

    [Fact]
    public async Task RemovePermissionAsync_CallsCorrectEndpoint()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        var request = handler.Expect(HttpMethod.Delete, $"/api/v1/boards/{boardId}/permissions/user1")
            .Respond(HttpStatusCode.NoContent);

        await svc.RemovePermissionAsync(boardId, "user1");

        handler.VerifyNoOutstandingExpectation();
    }
}
