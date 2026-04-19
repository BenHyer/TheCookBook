using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class IntegrationTestDbContext(DbContextOptions<IntegrationTestDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Recipe> Recipes => Set<Recipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>(entity =>
        {
            entity.ToTable("Boards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.OwnerUserId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => x.OwnerUserId);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.OwnerUserId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
        });
    }
}
