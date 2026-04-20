using System.Net;
using System.Net.Http.Json;
using Cookbook.Shared.Boards;
using CookbookMauiBlazor.Shared.Services;
using RichardSzalay.MockHttp;

namespace TestProject1;

public class BoardServiceTests
{
    private static (BoardService service, MockHttpMessageHandler handler) Create()
    {
        var handler = new MockHttpMessageHandler();
        var client = handler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");
        return (new BoardService(client), handler);
    }

    // --- GetByOwnerAsync ---

    [Fact]
    public async Task GetByOwnerAsync_OnSuccess_ReturnsBoards()
    {
        var (svc, handler) = Create();
        var boards = new List<BoardSummary> { new(Guid.NewGuid(), "Board A", "user1", DateTimeOffset.UtcNow) };
        handler.When("/api/v1/boards/owner/user1").Respond(HttpStatusCode.OK, JsonContent.Create(boards));

        var result = await svc.GetByOwnerAsync("user1");

        Assert.Equal(boards.Count, result.Count);
        Assert.Equal("Board A", result[0].Name);
    }

    [Fact]
    public async Task GetByOwnerAsync_OnHttpError_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/boards/owner/user1").Respond(HttpStatusCode.InternalServerError);

        var result = await svc.GetByOwnerAsync("user1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByOwnerAsync_OnException_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/boards/owner/user1").Throw(new HttpRequestException());

        var result = await svc.GetByOwnerAsync("user1");

        Assert.Empty(result);
    }

    // --- GetSharedAsync ---

    [Fact]
    public async Task GetSharedAsync_OnSuccess_ReturnsBoards()
    {
        var (svc, handler) = Create();
        var boards = new List<BoardSummary> { new(Guid.NewGuid(), "Shared", "other", DateTimeOffset.UtcNow) };
        handler.When("/api/v1/boards/shared-with-me/user1").Respond(HttpStatusCode.OK, JsonContent.Create(boards));

        var result = await svc.GetSharedAsync("user1");

        Assert.Single(result);
        Assert.Equal("Shared", result[0].Name);
    }

    [Fact]
    public async Task GetSharedAsync_OnHttpError_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/boards/shared-with-me/user1").Respond(HttpStatusCode.Forbidden);

        var result = await svc.GetSharedAsync("user1");

        Assert.Empty(result);
    }

    // --- GetDetailsAsync ---

    [Fact]
    public async Task GetDetailsAsync_OnSuccess_ReturnsBoardDetails()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        var details = new BoardDetails(boardId, "My Board", "desc", "user1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 3);
        handler.When($"/api/v1/boards/{boardId}").Respond(HttpStatusCode.OK, JsonContent.Create(details));

        var result = await svc.GetDetailsAsync(boardId);

        Assert.NotNull(result);
        Assert.Equal("My Board", result.Name);
        Assert.Equal(3, result.RecipeCount);
    }

    [Fact]
    public async Task GetDetailsAsync_OnException_ReturnsNull()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        handler.When($"/api/v1/boards/{boardId}").Respond(HttpStatusCode.NotFound);

        var result = await svc.GetDetailsAsync(boardId);

        Assert.Null(result);
    }

    // --- GetRecipesAsync ---

    [Fact]
    public async Task GetRecipesAsync_OnSuccess_ReturnsRecipes()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        var recipes = new List<BoardRecipeSummary> { new(1, "Pasta", null, null) };
        handler.When($"/api/v1/boards/{boardId}/recipes").Respond(HttpStatusCode.OK, JsonContent.Create(recipes));

        var result = await svc.GetRecipesAsync(boardId);

        Assert.Single(result);
        Assert.Equal("Pasta", result[0].Title);
    }

    [Fact]
    public async Task GetRecipesAsync_OnException_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        handler.When($"/api/v1/boards/{boardId}/recipes").Respond(HttpStatusCode.InternalServerError);

        var result = await svc.GetRecipesAsync(boardId);

        Assert.Empty(result);
    }

    // --- AddRecipesAsync ---

    [Fact]
    public async Task AddRecipesAsync_WithEmptyOwnerUserId_ReturnsEmpty()
    {
        var (svc, _) = Create();

        var result = await svc.AddRecipesAsync(Guid.NewGuid(), string.Empty, [1, 2]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddRecipesAsync_WithWhitespaceOwnerUserId_ReturnsEmpty()
    {
        var (svc, _) = Create();

        var result = await svc.AddRecipesAsync(Guid.NewGuid(), "   ", [1]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddRecipesAsync_WithEmptyRecipeIds_ReturnsEmpty()
    {
        var (svc, _) = Create();

        var result = await svc.AddRecipesAsync(Guid.NewGuid(), "user1", []);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddRecipesAsync_OnSuccess_ReturnsUpdatedRecipes()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        var updated = new List<BoardRecipeSummary> { new(1, "Pasta", null, null), new(2, "Soup", null, null) };
        handler.When(HttpMethod.Post, $"/api/v1/boards/{boardId}/recipes/bulk-add")
            .Respond(HttpStatusCode.OK, JsonContent.Create(updated));

        var result = await svc.AddRecipesAsync(boardId, "user1", [1, 2]);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddRecipesAsync_OnHttpError_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        var boardId = Guid.NewGuid();
        handler.When(HttpMethod.Post, $"/api/v1/boards/{boardId}/recipes/bulk-add")
            .Respond(HttpStatusCode.BadRequest);

        var result = await svc.AddRecipesAsync(boardId, "user1", [1]);

        Assert.Empty(result);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_WithBlankName_DoesNotCallApi()
    {
        var (svc, handler) = Create();
        handler.Fallback.Throw(new Exception("Should not call API"));

        await svc.CreateAsync("user1", "   ");

        // no exception = fallback was not hit
    }

    [Fact]
    public async Task CreateAsync_WithBlankOwnerUserId_DoesNotCallApi()
    {
        var (svc, handler) = Create();
        handler.Fallback.Throw(new Exception("Should not call API"));

        await svc.CreateAsync(string.Empty, "My Board");
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_CallsCorrectEndpoint()
    {
        var (svc, handler) = Create();
        var request = handler.Expect(HttpMethod.Post, "/api/v1/boards")
            .Respond(HttpStatusCode.Created);

        await svc.CreateAsync("user1", "My Board");

        handler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task CreateAsync_TrimsNameBeforePosting()
    {
        var (svc, handler) = Create();
        string? capturedBody = null;
        handler.When(HttpMethod.Post, "/api/v1/boards")
            .With(req => { capturedBody = req.Content?.ReadAsStringAsync().Result; return true; })
            .Respond(HttpStatusCode.Created);

        await svc.CreateAsync("user1", "  My Board  ");

        Assert.Contains("My Board", capturedBody);
        Assert.DoesNotContain("  My Board  ", capturedBody);
    }
}
