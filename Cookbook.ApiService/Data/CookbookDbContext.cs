using Cookbook.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace Cookbook.ApiService.Data;

public class CookbookDbContext(DbContextOptions<CookbookDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardPermission> BoardPermissions => Set<BoardPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.Description)
                .HasMaxLength(2000);
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.ToTable("Boards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.Description)
                .HasMaxLength(2000);
            entity.Property(x => x.OwnerUserId)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.CreatedUtc)
                .IsRequired();
            entity.Property(x => x.UpdatedUtc)
                .IsRequired();

            entity.HasIndex(x => x.OwnerUserId);
        });

        modelBuilder.Entity<BoardPermission>(entity =>
        {
            entity.ToTable("BoardPermissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.Role)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(x => x.CreatedUtc)
                .IsRequired();

            entity.HasOne(x => x.Board)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.BoardId, x.UserId })
                .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
