using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

public class BoardPermissionIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public BoardPermissionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static Board MakeBoard(string owner = "owner-1")
    {
        var utcNow = DateTime.UtcNow;
        return new Board { Name = "Test Board", OwnerUserId = owner, CreatedUtc = utcNow, UpdatedUtc = utcNow };
    }

    private static BoardPermission MakePermission(Guid boardId, string userId, string role = "Admin") =>
        new() { BoardId = boardId, UserId = userId, Role = role, CreatedUtc = DateTime.UtcNow };

    [Fact]
    public async Task CreateBoardPermission_CanBeReadBack()
    {
        await using var db = _fixture.CreateContext();
        var board = MakeBoard();
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        db.BoardPermissions.Add(MakePermission(board.Id, "user-1", "Admin"));
        await db.SaveChangesAsync();

        var permission = await db.BoardPermissions
            .FirstOrDefaultAsync(p => p.BoardId == board.Id && p.UserId == "user-1");

        Assert.NotNull(permission);
        Assert.Equal("Admin", permission.Role);
    }

    [Fact]
    public async Task BoardPermissions_UniqueConstraint_PreventsUserDuplicate()
    {
        await using var db = _fixture.CreateContext();
        var board = MakeBoard();
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        db.BoardPermissions.Add(MakePermission(board.Id, "user-1", "Admin"));
        await db.SaveChangesAsync();

        db.BoardPermissions.Add(MakePermission(board.Id, "user-1", "Viewer"));

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task MultipleUsers_CanHavePermissionsOnSameBoard()
    {
        await using var db = _fixture.CreateContext();
        var board = MakeBoard();
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        db.BoardPermissions.AddRange(
            MakePermission(board.Id, "owner-1", "Admin"),
            MakePermission(board.Id, "viewer-1", "Viewer"),
            MakePermission(board.Id, "editor-1", "Editor")
        );
        await db.SaveChangesAsync();

        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Equal(3, permissions.Count);
        Assert.Contains(permissions, p => p.Role == "Admin");
        Assert.Contains(permissions, p => p.Role == "Viewer");
        Assert.Contains(permissions, p => p.Role == "Editor");
    }

    [Fact]
    public async Task DeleteBoard_CascadesDeleteToPermissions()
    {
        await using var db = _fixture.CreateContext();
        var board = MakeBoard();
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        db.BoardPermissions.AddRange(
            MakePermission(board.Id, "owner-1", "Admin"),
            MakePermission(board.Id, "viewer-1", "Viewer")
        );
        await db.SaveChangesAsync();

        db.Boards.Remove(board);
        await db.SaveChangesAsync();

        var orphanedPermissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Empty(orphanedPermissions);
    }

    [Fact]
    public async Task SameUser_CanHavePermissionsOnDifferentBoards()
    {
        await using var db = _fixture.CreateContext();
        var utcNow = DateTime.UtcNow;
        var board1 = new Board { Name = "Board 1", OwnerUserId = "owner-1", CreatedUtc = utcNow, UpdatedUtc = utcNow };
        var board2 = new Board { Name = "Board 2", OwnerUserId = "owner-2", CreatedUtc = utcNow, UpdatedUtc = utcNow };
        db.Boards.AddRange(board1, board2);
        await db.SaveChangesAsync();

        db.BoardPermissions.AddRange(
            MakePermission(board1.Id, "shared-user", "Viewer"),
            MakePermission(board2.Id, "shared-user", "Editor")
        );
        await db.SaveChangesAsync();

        var userPermissions = await db.BoardPermissions
            .Where(p => p.UserId == "shared-user")
            .ToListAsync();

        Assert.Equal(2, userPermissions.Count);
    }
}
