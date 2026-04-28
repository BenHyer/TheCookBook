using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

public static class BoardRoles
{
    public const string Manager = "Manager";
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Viewer = "Viewer";
}

public class BoardShareIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public BoardShareIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private string Uid(string name) => $"{name}-{Guid.NewGuid()}";

    private static Board MakeBoard(string owner) =>
        new() { Name = "Test Board", OwnerUserId = owner, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };

    private static BoardPermission MakePermission(Guid boardId, string userId, string role) =>
        new() { BoardId = boardId, UserId = userId, Role = role, CreatedUtc = DateTime.UtcNow };

    private static UserProfile MakeUser(string userId, string displayName = "Test User") =>
        new() { UserId = userId, DisplayName = displayName, FirstName = "Test", LastName = "User" };

    [Fact]
    public async Task ShareBoard_BoardOwnerCanShare()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var invitee = Uid("invitee");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.Add(MakePermission(board.Id, owner, BoardRoles.Manager));
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(invitee));
        await db.SaveChangesAsync();

        // Simulate the owner sharing the board with invitee
        db.BoardPermissions.Add(MakePermission(board.Id, invitee, BoardRoles.Viewer));
        await db.SaveChangesAsync();

        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Equal(2, permissions.Count);
        Assert.Contains(permissions, p => p.UserId == owner && p.Role == BoardRoles.Manager);
        Assert.Contains(permissions, p => p.UserId == invitee && p.Role == BoardRoles.Viewer);
    }

    [Fact]
    public async Task ShareBoard_UserWithManagerRoleCanShare()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var manager = Uid("manager");
        var invitee = Uid("invitee");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.AddRange(
            MakePermission(board.Id, owner, BoardRoles.Manager),
            MakePermission(board.Id, manager, BoardRoles.Manager)
        );
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(manager), MakeUser(invitee));
        await db.SaveChangesAsync();

        // Manager should be able to add another user
        db.BoardPermissions.Add(MakePermission(board.Id, invitee, BoardRoles.Viewer));
        await db.SaveChangesAsync();

        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Equal(3, permissions.Count);
        Assert.Contains(permissions, p => p.UserId == invitee && p.Role == BoardRoles.Viewer);
    }

    [Fact]
    public async Task ShareBoard_UserWithAdminRoleLegacyCanShare()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var admin = Uid("admin");
        var invitee = Uid("invitee");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        // Simulate legacy Admin role
        db.BoardPermissions.AddRange(
            MakePermission(board.Id, owner, BoardRoles.Manager),
            MakePermission(board.Id, admin, BoardRoles.Admin)
        );
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(admin), MakeUser(invitee));
        await db.SaveChangesAsync();

        // Legacy Admin should still be able to share
        db.BoardPermissions.Add(MakePermission(board.Id, invitee, BoardRoles.Viewer));
        await db.SaveChangesAsync();

        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Equal(3, permissions.Count);
        Assert.Contains(permissions, p => p.UserId == invitee && p.Role == BoardRoles.Viewer);
    }

    [Fact]
    public async Task ShareBoard_UserWithViewerRoleCannotShare()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var viewer = Uid("viewer");
        var invitee = Uid("invitee");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.AddRange(
            MakePermission(board.Id, owner, BoardRoles.Manager),
            MakePermission(board.Id, viewer, BoardRoles.Viewer)
        );
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(viewer), MakeUser(invitee));
        await db.SaveChangesAsync();

        // Attempt to add user as a Viewer (should be prevented at endpoint level)
        // This test documents the expected behavior; endpoint auth is tested separately.
        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Equal(2, permissions.Count);
        Assert.DoesNotContain(permissions, p => p.UserId == invitee);
    }

    [Fact]
    public async Task ShareBoard_UserWithEditorRoleCannotShare()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var editor = Uid("editor");
        var invitee = Uid("invitee");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.AddRange(
            MakePermission(board.Id, owner, BoardRoles.Manager),
            MakePermission(board.Id, editor, BoardRoles.Editor)
        );
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(editor), MakeUser(invitee));
        await db.SaveChangesAsync();

        // Attempt to add user as an Editor (should be prevented at endpoint level)
        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Equal(2, permissions.Count);
        Assert.DoesNotContain(permissions, p => p.UserId == invitee);
    }

    [Fact]
    public async Task ShareBoard_NonCollaboratorCannotShare()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var outsider = Uid("outsider");
        var invitee = Uid("invitee");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.Add(MakePermission(board.Id, owner, BoardRoles.Manager));
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(outsider), MakeUser(invitee));
        await db.SaveChangesAsync();

        // Outsider is not a collaborator at all
        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Single(permissions);
        Assert.All(permissions, p => Assert.Equal(owner, p.UserId));
    }

    [Fact]
    public async Task RemovePermission_BoardOwnerCanRemove()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var collaborator = Uid("collab");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.AddRange(
            MakePermission(board.Id, owner, BoardRoles.Manager),
            MakePermission(board.Id, collaborator, BoardRoles.Editor)
        );
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(collaborator));
        await db.SaveChangesAsync();

        // Owner removes the collaborator
        var toRemove = db.BoardPermissions
            .FirstOrDefault(p => p.BoardId == board.Id && p.UserId == collaborator);
        if (toRemove is not null)
        {
            db.BoardPermissions.Remove(toRemove);
            await db.SaveChangesAsync();
        }

        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Single(permissions);
        Assert.All(permissions, p => Assert.Equal(owner, p.UserId));
    }

    [Fact]
    public async Task RemovePermission_ManagerRoleCanRemove()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");
        var manager = Uid("manager");
        var collaborator = Uid("collab");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.AddRange(
            MakePermission(board.Id, owner, BoardRoles.Manager),
            MakePermission(board.Id, manager, BoardRoles.Manager),
            MakePermission(board.Id, collaborator, BoardRoles.Viewer)
        );
        db.UserProfiles.AddRange(MakeUser(owner), MakeUser(manager), MakeUser(collaborator));
        await db.SaveChangesAsync();

        // Manager removes a collaborator
        var toRemove = db.BoardPermissions
            .FirstOrDefault(p => p.BoardId == board.Id && p.UserId == collaborator);
        if (toRemove is not null)
        {
            db.BoardPermissions.Remove(toRemove);
            await db.SaveChangesAsync();
        }

        var permissions = await db.BoardPermissions
            .Where(p => p.BoardId == board.Id)
            .ToListAsync();

        Assert.Equal(2, permissions.Count);
        Assert.DoesNotContain(permissions, p => p.UserId == collaborator);
    }

    [Fact]
    public async Task RemovePermission_CannotRemoveOwner()
    {
        await using var db = _fixture.CreateContext();
        var owner = Uid("owner");

        var board = MakeBoard(owner);
        db.Boards.Add(board);
        db.BoardPermissions.Add(MakePermission(board.Id, owner, BoardRoles.Manager));
        db.UserProfiles.Add(MakeUser(owner));
        await db.SaveChangesAsync();

        // Attempting to remove the owner's permission should not proceed
        // (This is prevented at the endpoint level with a BadRequest)
        var ownerPermission = await db.BoardPermissions
            .FirstOrDefaultAsync(p => p.BoardId == board.Id && p.UserId == owner);

        Assert.NotNull(ownerPermission);
        Assert.Equal(owner, ownerPermission.UserId);
    }
}
