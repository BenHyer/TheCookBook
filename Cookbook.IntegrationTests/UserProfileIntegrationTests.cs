namespace Cookbook.IntegrationTests;

public class UserProfileIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserProfileIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static UserProfile MakeProfile(string userId = "user-1") => new()
    {
        UserId = userId,
        FirstName = "John",
        LastName = "Doe",
        DisplayName = "John Doe",
        CreatedUtc = DateTime.UtcNow,
        UpdatedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateProfile_CanBeReadBack()
    {
        await using var db = _fixture.CreateContext();

        db.UserProfiles.Add(MakeProfile("user-1"));
        await db.SaveChangesAsync();

        var loaded = await db.UserProfiles.FindAsync("user-1");

        Assert.NotNull(loaded);
        Assert.Equal("John", loaded.FirstName);
        Assert.Equal("Doe", loaded.LastName);
        Assert.Equal("John Doe", loaded.DisplayName);
    }

    [Fact]
    public async Task CreateProfile_WhenNotFound_FindAsyncReturnsNull()
    {
        await using var db = _fixture.CreateContext();

        var loaded = await db.UserProfiles.FindAsync("no-such-user");

        Assert.Null(loaded);
    }

    [Fact]
    public async Task UpdateProfile_ChangesArePersisted()
    {
        await using var db = _fixture.CreateContext();

        var profile = MakeProfile("user-2");
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        profile.FirstName = "Jane";
        profile.DisplayName = "Jane Doe";
        profile.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.UserProfiles.FindAsync("user-2");

        Assert.NotNull(loaded);
        Assert.Equal("Jane", loaded.FirstName);
        Assert.Equal("Jane Doe", loaded.DisplayName);
    }

    [Fact]
    public async Task CreateProfile_WithProfilePictureUrl_PersistsCorrectly()
    {
        await using var db = _fixture.CreateContext();

        var profile = MakeProfile("user-3");
        profile.ProfilePictureUrl = "https://cdn.example.com/pic.jpg";
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.UserProfiles.FindAsync("user-3");

        Assert.Equal("https://cdn.example.com/pic.jpg", loaded!.ProfilePictureUrl);
    }

    [Fact]
    public async Task CreateProfile_WithNullProfilePictureUrl_PersistsNullCorrectly()
    {
        await using var db = _fixture.CreateContext();

        db.UserProfiles.Add(MakeProfile("user-4"));
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.UserProfiles.FindAsync("user-4");

        Assert.Null(loaded!.ProfilePictureUrl);
    }

    [Fact]
    public async Task DuplicateUserId_ThrowsDatabaseException()
    {
        await using var db = _fixture.CreateContext();
        db.UserProfiles.Add(MakeProfile("user-5"));
        await db.SaveChangesAsync();

        // Use a fresh context so EF's identity map doesn't prevent reaching the DB constraint.
        await using var db2 = _fixture.CreateContext();
        db2.UserProfiles.Add(MakeProfile("user-5"));
        await Assert.ThrowsAnyAsync<Exception>(() => db2.SaveChangesAsync());
    }
}
