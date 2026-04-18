using Cookbook.ApiService.Data;
using Cookbook.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Cookbook.ApiService.ApiHelpers;

namespace Cookbook.ApiService.Endpoints;

public static class BoardSharingEndpoints
{
    public static WebApplication MapBoardSharingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/users", async (string? search, CookbookDbContext dbContext) =>
        {
            var query = dbContext.UserProfiles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(u =>
                    u.DisplayName.ToLower().Contains(searchLower) ||
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower));
            }

            var users = await query
                .OrderBy(u => u.DisplayName)
                .Select(u => new UserSummaryDto(u.UserId, u.DisplayName, u.FirstName, u.LastName, u.ProfilePictureUrl))
                .Take(50)
                .ToListAsync();

            return Results.Ok(users);
        })
        .WithName("SearchUsers");

        app.MapGet("/api/v1/boards/{boardId:guid}/collaborators", async (Guid boardId, CookbookDbContext dbContext) =>
        {
            var board = await dbContext.Boards
                .Include(b => b.Permissions)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board is null)
                return Results.NotFound();

            var permissions = await dbContext.BoardPermissions
                .Where(bp => bp.BoardId == boardId && bp.Status == BoardPermissionStatus.Active)
                .Join(dbContext.UserProfiles,
                    bp => bp.UserId,
                    up => up.UserId,
                    (bp, up) => new BoardCollaborator(up.UserId, up.DisplayName, up.FirstName, up.LastName, up.ProfilePictureUrl, bp.Role))
                .ToListAsync();

            return Results.Ok(permissions);
        })
        .WithName("GetBoardCollaborators");

        app.MapPost("/api/v1/boards/{boardId:guid}/share", async (
            Guid boardId,
            ShareBoardRequest request,
            ClaimsPrincipal user,
            CookbookDbContext dbContext,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("BoardSharing");
            logger.LogInformation("[ShareBoard] Request for boardId={BoardId}, userIds={UserIds}, role={Role}",
                boardId, string.Join(",", request.UserIds ?? []), request.Role);

            var board = await dbContext.Boards
                .Include(b => b.Permissions)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board is null)
            {
                logger.LogWarning("[ShareBoard] Board {BoardId} not found", boardId);
                return Results.NotFound();
            }

            var currentUserId = user.FindFirst("oid")?.Value ??
                user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                user.FindFirst("sub")?.Value;

            logger.LogInformation("[ShareBoard] Resolved currentUserId={CurrentUserId}", currentUserId ?? "null");

            if (currentUserId is null)
            {
                logger.LogWarning("[ShareBoard] Could not resolve caller identity from claims");
                return Results.Forbid();
            }

            var userPermission = board.Permissions.FirstOrDefault(p => p.UserId == currentUserId && p.Status == BoardPermissionStatus.Active);
            logger.LogInformation("[ShareBoard] Caller permission: role={Role}, status={Status}",
                userPermission?.Role ?? "none", userPermission?.Status ?? "none");

            if (userPermission?.Role != BoardRoles.Admin)
            {
                logger.LogWarning("[ShareBoard] Caller {CurrentUserId} is not Admin on board {BoardId}", currentUserId, boardId);
                return Results.Forbid();
            }

            var role = request.Role is BoardRoles.Viewer or BoardRoles.Editor ? request.Role : BoardRoles.Viewer;
            var utcNow = DateTime.UtcNow;
            var existingPermissionUserIds = board.Permissions.Select(p => p.UserId).ToHashSet();

            foreach (var userId in request.UserIds)
            {
                if (existingPermissionUserIds.Contains(userId) || userId == currentUserId)
                {
                    logger.LogInformation("[ShareBoard] Skipping userId={UserId} (already has permission or is caller)", userId);
                    continue;
                }

                var userExists = await dbContext.UserProfiles.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    logger.LogWarning("[ShareBoard] Skipping userId={UserId} — no UserProfile found", userId);
                    continue;
                }

                logger.LogInformation("[ShareBoard] Adding Pending permission for userId={UserId}, role={Role}", userId, role);
                dbContext.BoardPermissions.Add(new BoardPermission
                {
                    BoardId = boardId,
                    UserId = userId,
                    Role = role,
                    Status = BoardPermissionStatus.Pending,
                    CreatedUtc = utcNow
                });
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("[ShareBoard] SaveChanges completed for boardId={BoardId}", boardId);

            var collaborators = await dbContext.BoardPermissions
                .Where(bp => bp.BoardId == boardId && bp.Status == BoardPermissionStatus.Active)
                .Join(dbContext.UserProfiles,
                    bp => bp.UserId,
                    up => up.UserId,
                    (bp, up) => new BoardCollaborator(up.UserId, up.DisplayName, up.FirstName, up.LastName, up.ProfilePictureUrl, bp.Role))
                .ToListAsync();

            return Results.Ok(collaborators);
        })
        .WithName("ShareBoard")
        .RequireAuthorization();

        app.MapDelete("/api/v1/boards/{boardId:guid}/permissions/{userId}", async (
            Guid boardId,
            string userId,
            ClaimsPrincipal user,
            CookbookDbContext dbContext) =>
        {
            var board = await dbContext.Boards
                .Include(b => b.Permissions)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board is null)
                return Results.NotFound();

            var currentUserId = user.FindFirst("oid")?.Value ??
                user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                user.FindFirst("sub")?.Value;

            if (currentUserId is null)
                return Results.Forbid();

            var currentUserPermission = board.Permissions.FirstOrDefault(p => p.UserId == currentUserId && p.Status == BoardPermissionStatus.Active);
            if (currentUserPermission?.Role != BoardRoles.Admin)
                return Results.Forbid();

            if (userId == board.OwnerUserId && currentUserPermission?.UserId == userId)
                return Results.BadRequest("Cannot remove the board owner's permissions.");

            var permission = await dbContext.BoardPermissions
                .FirstOrDefaultAsync(bp => bp.BoardId == boardId && bp.UserId == userId);

            if (permission is null)
                return Results.NotFound();

            dbContext.BoardPermissions.Remove(permission);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        })
        .WithName("RemovePermission")
        .RequireAuthorization();

        app.MapGet("/api/v1/boards/shared-with-me/{userId}", async (string userId, CookbookDbContext dbContext) =>
        {
            var sharedBoards = await dbContext.BoardPermissions
                .Where(bp => bp.UserId == userId && bp.Status == BoardPermissionStatus.Active)
                .Join(dbContext.Boards,
                    bp => bp.BoardId,
                    b => b.Id,
                    (bp, b) => new { Permission = bp, Board = b })
                .Where(x => x.Board.OwnerUserId != userId)
                .ToListAsync();

            var result = sharedBoards
                .Select(x => new SharedBoardSummary(
                    x.Board.Id,
                    x.Board.Name,
                    x.Board.OwnerUserId,
                    x.Permission.Role,
                    ToUtcOffset(x.Board.CreatedUtc)))
                .OrderBy(b => b.Name)
                .ToList();

            return Results.Ok(result);
        })
        .WithName("GetSharedBoards");

        app.MapGet("/api/v1/users/{userId}/invitations", async (string userId, CookbookDbContext dbContext) =>
        {
            var raw = await dbContext.BoardPermissions
                .Where(bp => bp.UserId == userId && bp.Status == BoardPermissionStatus.Pending)
                .Join(dbContext.Boards,
                    bp => bp.BoardId,
                    b => b.Id,
                    (bp, b) => new { Permission = bp, Board = b })
                .Join(dbContext.UserProfiles,
                    x => x.Board.OwnerUserId,
                    up => up.UserId,
                    (x, up) => new
                    {
                        BoardId = x.Board.Id,
                        BoardName = x.Board.Name,
                        OwnerUserId = x.Board.OwnerUserId,
                        OwnerDisplayName = up.DisplayName,
                        Role = x.Permission.Role,
                        CreatedUtc = x.Permission.CreatedUtc
                    })
                .OrderByDescending(i => i.CreatedUtc)
                .ToListAsync();

            var invitations = raw
                .Select(x => new BoardInvitationDto(
                    x.BoardId,
                    x.BoardName,
                    x.OwnerUserId,
                    x.OwnerDisplayName,
                    x.Role,
                    ToUtcOffset(x.CreatedUtc)))
                .ToList();

            return Results.Ok(invitations);
        })
        .WithName("GetPendingInvitations");

        app.MapPost("/api/v1/boards/{boardId:guid}/invitations/accept", async (
            Guid boardId,
            ClaimsPrincipal user,
            CookbookDbContext dbContext) =>
        {
            var currentUserId = user.FindFirst("oid")?.Value ??
                user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                user.FindFirst("sub")?.Value;

            if (currentUserId is null) return Results.Forbid();

            var permission = await dbContext.BoardPermissions
                .FirstOrDefaultAsync(bp => bp.BoardId == boardId && bp.UserId == currentUserId && bp.Status == BoardPermissionStatus.Pending);

            if (permission is null) return Results.NotFound();

            permission.Status = BoardPermissionStatus.Active;
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        })
        .WithName("AcceptInvitation")
        .RequireAuthorization();

        app.MapPost("/api/v1/boards/{boardId:guid}/invitations/reject", async (
            Guid boardId,
            ClaimsPrincipal user,
            CookbookDbContext dbContext) =>
        {
            var currentUserId = user.FindFirst("oid")?.Value ??
                user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                user.FindFirst("sub")?.Value;

            if (currentUserId is null) return Results.Forbid();

            var permission = await dbContext.BoardPermissions
                .FirstOrDefaultAsync(bp => bp.BoardId == boardId && bp.UserId == currentUserId && bp.Status == BoardPermissionStatus.Pending);

            if (permission is null) return Results.NotFound();

            dbContext.BoardPermissions.Remove(permission);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        })
        .WithName("RejectInvitation")
        .RequireAuthorization();

        return app;
    }
}

record UserSummaryDto(string UserId, string DisplayName, string FirstName, string LastName, string? ProfilePictureUrl);
record ShareBoardRequest(List<string> UserIds, string? Role);
record BoardCollaborator(string UserId, string DisplayName, string FirstName, string LastName, string? ProfilePictureUrl, string Role);
record SharedBoardSummary(Guid Id, string Name, string OwnerUserId, string Role, DateTimeOffset CreatedAt);
record BoardInvitationDto(Guid BoardId, string BoardName, string OwnerUserId, string OwnerDisplayName, string Role, DateTimeOffset InvitedAt);
