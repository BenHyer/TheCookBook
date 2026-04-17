using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Cookbook.ApiService.Data;
using Cookbook.ApiService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cookbook.ApiService.Endpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(
        this WebApplication app,
        bool enableProfiles,
        bool enablePictures)
    {
        if (enableProfiles)
        {
            app.MapGet("/api/v1/users/{userId}/profile", async (string userId, CookbookDbContext dbContext) =>
            {
                var profile = await dbContext.UserProfiles.FindAsync(userId);
                if (profile is null)
                    return Results.NotFound();

                return Results.Ok(new UserProfileDto(
                    profile.UserId,
                    profile.FirstName,
                    profile.LastName,
                    profile.DisplayName,
                    profile.ProfilePictureUrl));
            })
            .WithName("GetUserProfile");

            app.MapPut("/api/v1/users/{userId}/profile", async (
                string userId,
                UpdateProfileRequest request,
                CookbookDbContext dbContext) =>
            {
                var utcNow = DateTime.UtcNow;
                var profile = await dbContext.UserProfiles.FindAsync(userId);

                if (profile is null)
                {
                    var firstName = request.FirstName.Trim();
                    var lastName = request.LastName.Trim();
                    profile = new UserProfile
                    {
                        UserId = userId,
                        FirstName = firstName,
                        LastName = lastName,
                        DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                            ? $"{firstName} {lastName}".Trim()
                            : request.DisplayName.Trim(),
                        CreatedUtc = utcNow,
                        UpdatedUtc = utcNow
                    };
                    dbContext.UserProfiles.Add(profile);
                }
                else
                {
                    profile.FirstName = request.FirstName.Trim();
                    profile.LastName = request.LastName.Trim();
                    profile.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                        ? $"{request.FirstName.Trim()} {request.LastName.Trim()}".Trim()
                        : request.DisplayName.Trim();
                    profile.UpdatedUtc = utcNow;
                }

                await dbContext.SaveChangesAsync();

                return Results.Ok(new UserProfileDto(
                    profile.UserId,
                    profile.FirstName,
                    profile.LastName,
                    profile.DisplayName,
                    profile.ProfilePictureUrl));
            })
            .WithName("UpdateUserProfile");

            app.MapPost("/api/v1/users/ensure-profile", async (
                ClaimsPrincipal user,
                CookbookDbContext dbContext) =>
            {
                var userId = user.FindFirst("oid")?.Value ??
                    user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                    user.FindFirst("sub")?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                    return Results.Unauthorized();

                var profile = await dbContext.UserProfiles.FindAsync(userId);
                if (profile is not null)
                {
                    return Results.Ok(new UserProfileDto(
                        profile.UserId,
                        profile.FirstName,
                        profile.LastName,
                        profile.DisplayName,
                        profile.ProfilePictureUrl));
                }

                var givenName = user.FindFirst("given_name")?.Value ?? string.Empty;
                var surname = user.FindFirst("family_name")?.Value ?? string.Empty;
                var name = user.FindFirst("name")?.Value ?? string.Empty;
                var displayName = !string.IsNullOrWhiteSpace(name) ? name : $"{givenName} {surname}".Trim();

                var utcNow = DateTime.UtcNow;
                profile = new UserProfile
                {
                    UserId = userId,
                    FirstName = givenName,
                    LastName = surname,
                    DisplayName = displayName,
                    CreatedUtc = utcNow,
                    UpdatedUtc = utcNow
                };

                dbContext.UserProfiles.Add(profile);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new UserProfileDto(
                    profile.UserId,
                    profile.FirstName,
                    profile.LastName,
                    profile.DisplayName,
                    profile.ProfilePictureUrl));
            })
            .WithName("EnsureUserProfile");
        }
        else
        {
            app.Logger.LogInformation("Feature flag disabled: user profile endpoints.");
        }

        if (enableProfiles && enablePictures)
        {
            app.MapPost("/api/v1/users/{userId}/profile/picture", async (
                string userId,
                IFormFile file,
                CookbookDbContext dbContext,
                [FromServices] BlobContainerClient? containerClient) =>
            {
                if (containerClient is null)
                    return Results.Problem("Blob storage is not configured.");

                if (file.Length == 0)
                    return Results.BadRequest("No file uploaded.");

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                    return Results.BadRequest("Only JPEG, PNG, GIF, and WebP images are allowed.");

                if (file.Length > 5 * 1024 * 1024)
                    return Results.BadRequest("File size must be under 5 MB.");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var blobName = $"{userId}/profile{extension}";
                var blobClient = containerClient.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                });

                var pictureUrl = blobClient.Uri.ToString();
                var utcNow = DateTime.UtcNow;

                var profile = await dbContext.UserProfiles.FindAsync(userId);
                if (profile is null)
                {
                    profile = new UserProfile
                    {
                        UserId = userId,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        DisplayName = string.Empty,
                        ProfilePictureUrl = pictureUrl,
                        CreatedUtc = utcNow,
                        UpdatedUtc = utcNow
                    };
                    dbContext.UserProfiles.Add(profile);
                }
                else
                {
                    profile.ProfilePictureUrl = pictureUrl;
                    profile.UpdatedUtc = utcNow;
                }

                await dbContext.SaveChangesAsync();

                return Results.Ok(new { url = pictureUrl });
            })
            .WithName("UploadProfilePicture")
            .DisableAntiforgery();
        }
        else
        {
            app.Logger.LogInformation("Feature flag disabled: profile picture uploads.");
        }

        return app;
    }
}

record UserProfileDto(string UserId, string FirstName, string LastName, string DisplayName, string? ProfilePictureUrl);
record UpdateProfileRequest(string FirstName, string LastName, string DisplayName);
