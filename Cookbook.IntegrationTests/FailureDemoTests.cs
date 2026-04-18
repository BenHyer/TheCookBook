namespace Cookbook.IntegrationTests;

/// <summary>
/// This file exists solely to capture a "failing integration test" screenshot for the assignment.
///
/// HOW TO USE:
///   1. Push with the [Fact] attribute active → pipeline fails → take screenshot.
///   2. Change [Fact] to [Fact(Skip = "demo only")] → push again → all tests pass → take screenshot.
/// </summary>
public class FailureDemoTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public FailureDemoTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // Change to [Fact(Skip = "demo only")] after capturing the failing screenshot.
    [Fact]
    public async Task Demo_IntentionalFailure_BoardNameShouldNotBeEmpty()
    {
        await using var db = _fixture.CreateContext();

        var utcNow = DateTime.UtcNow;
        var board = new Board
        {
            Name = "Real Board",
            OwnerUserId = "demo-user",
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow,
        };
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var loaded = await db.Boards.FindAsync(board.Id);

        // This assertion is intentionally wrong to produce a visible failure.
        Assert.Equal(string.Empty, loaded!.Name);
    }
}
