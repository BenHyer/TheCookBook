namespace Cookbook.IntegrationTests;


public class FailureDemoTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public FailureDemoTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "demo only")]
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

        Assert.Equal(string.Empty, loaded!.Name);
    }
}
