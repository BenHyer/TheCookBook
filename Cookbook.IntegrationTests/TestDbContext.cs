using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cookbook.IntegrationTests;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<BoardPermission> Permissions { get; set; } = new List<BoardPermission>();
    public ICollection<BoardRecipe> BoardRecipes { get; set; } = new List<BoardRecipe>();
}

public class Recipe
{
    public int Id { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? YieldServings { get; set; }
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public string? TotalTime { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public List<string> Quantities { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public string? CookingTemperature { get; set; }
    public string? NutritionFacts { get; set; }
    public string? StorageInfo { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<BoardRecipe> BoardRecipes { get; set; } = new List<BoardRecipe>();
}

public class BoardPermission
{
    public int Id { get; set; }
    public Guid BoardId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public DateTime CreatedUtc { get; set; }

    public Board Board { get; set; } = null!;
}

public class BoardRecipe
{
    public Guid BoardId { get; set; }
    public int RecipeId { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Board Board { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class IntegrationTestDbContext(DbContextOptions<IntegrationTestDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<BoardPermission> BoardPermissions => Set<BoardPermission>();
    public DbSet<BoardRecipe> BoardRecipes => Set<BoardRecipe>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var listConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>());

        var listComparer = new ValueComparer<List<string>>(
            (l, r) => l == null && r == null || l != null && r != null && l.SequenceEqual(r),
            v => v == null ? 0 : v.Aggregate(0, (h, i) => HashCode.Combine(h, i == null ? 0 : i.GetHashCode())),
            v => v == null ? new List<string>() : v.ToList());

        modelBuilder.Entity<Board>(e =>
        {
            e.ToTable("Boards");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.OwnerUserId).IsRequired().HasMaxLength(200);
            e.Property(x => x.CreatedUtc).IsRequired();
            e.Property(x => x.UpdatedUtc).IsRequired();
            e.HasIndex(x => x.OwnerUserId);
        });

        modelBuilder.Entity<Recipe>(e =>
        {
            e.ToTable("Recipes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.OwnerUserId).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.YieldServings).HasMaxLength(200);
            e.Property(x => x.PrepTime).HasMaxLength(200);
            e.Property(x => x.CookTime).HasMaxLength(200);
            e.Property(x => x.TotalTime).HasMaxLength(200);
            e.Property(x => x.CookingTemperature).HasMaxLength(200);
            e.Property(x => x.NutritionFacts).HasMaxLength(2000);
            e.Property(x => x.StorageInfo).HasMaxLength(2000);
            e.Property(x => x.ImageUrl).HasMaxLength(2048);
            e.HasIndex(x => x.OwnerUserId);
            e.Property(x => x.Ingredients).HasConversion(listConverter).Metadata.SetValueComparer(listComparer);
            e.Property(x => x.Quantities).HasConversion(listConverter).Metadata.SetValueComparer(listComparer);
            e.Property(x => x.Equipment).HasConversion(listConverter).Metadata.SetValueComparer(listComparer);
            e.Property(x => x.Instructions).HasConversion(listConverter).Metadata.SetValueComparer(listComparer);
        });

        modelBuilder.Entity<BoardPermission>(e =>
        {
            e.ToTable("BoardPermissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(200);
            e.Property(x => x.Role).IsRequired().HasMaxLength(20);
            e.Property(x => x.CreatedUtc).IsRequired();
            e.HasOne(x => x.Board)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.BoardId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<BoardRecipe>(e =>
        {
            e.ToTable("BoardRecipes");
            e.HasKey(x => new { x.BoardId, x.RecipeId });
            e.Property(x => x.CreatedUtc).IsRequired();
            e.HasOne(x => x.Board)
                .WithMany(x => x.BoardRecipes)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Recipe)
                .WithMany(x => x.BoardRecipes)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.RecipeId);
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.ToTable("UserProfiles");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(200);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ProfilePictureUrl).HasMaxLength(2048);
            e.Property(x => x.CreatedUtc).IsRequired();
            e.Property(x => x.UpdatedUtc).IsRequired();
        });
    }
}
