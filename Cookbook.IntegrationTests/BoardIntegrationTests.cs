using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

public class BoardIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public BoardIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateBoard_CanBeReadBack()
    {
        await using var db = _fixture.CreateContext();

        var board = new Board
        {
            Name = "Test Board",
            OwnerUserId = "user-001",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var loaded = await db.Boards.FindAsync(board.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Test Board", loaded.Name);
        Assert.Equal("user-001", loaded.OwnerUserId);
    }

    [Fact]
    public async Task GetBoardsByOwner_ReturnsOnlyThatOwnersBoards()
    {
        await using var db = _fixture.CreateContext();

        var utcNow = DateTime.UtcNow;
        db.Boards.AddRange(
            new Board { Name = "Alice Board 1", OwnerUserId = "alice", CreatedUtc = utcNow, UpdatedUtc = utcNow },
            new Board { Name = "Alice Board 2", OwnerUserId = "alice", CreatedUtc = utcNow, UpdatedUtc = utcNow },
            new Board { Name = "Bob Board",    OwnerUserId = "bob",   CreatedUtc = utcNow, UpdatedUtc = utcNow }
        );
        await db.SaveChangesAsync();

        var aliceBoards = await db.Boards
            .Where(b => b.OwnerUserId == "alice")
            .ToListAsync();

        Assert.Equal(2, aliceBoards.Count);
        Assert.All(aliceBoards, b => Assert.Equal("alice", b.OwnerUserId));
    }

    [Fact]
    public async Task CreateBoard_WithOptionalDescription_PersistsCorrectly()
    {
        await using var db = _fixture.CreateContext();

        var utcNow = DateTime.UtcNow;
        var withDesc = new Board
        {
            Name = "Described Board",
            Description = "A board with a description",
            OwnerUserId = "user-002",
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow,
        };
        var withoutDesc = new Board
        {
            Name = "Plain Board",
            OwnerUserId = "user-002",
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow,
        };

        db.Boards.AddRange(withDesc, withoutDesc);
        await db.SaveChangesAsync();

        var loaded1 = await db.Boards.FindAsync(withDesc.Id);
        var loaded2 = await db.Boards.FindAsync(withoutDesc.Id);

        Assert.NotNull(loaded1?.Description);
        Assert.Null(loaded2?.Description);
    }

    [Fact]
    public async Task CreateBoard_WithNullName_ThrowsDatabaseException()
    {
        await using var db = _fixture.CreateContext();

        // Name is required (NOT NULL in schema); setting it to null should fail on SaveChanges.
        var invalidBoard = new Board
        {
            Name = null!,
            OwnerUserId = "user-003",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

        db.Boards.Add(invalidBoard);

        // This test intentionally demonstrates a failure path:
        // inserting a row that violates the NOT NULL constraint raises an exception.
        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }
}
