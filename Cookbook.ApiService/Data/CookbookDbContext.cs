using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cookbook.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cookbook.ApiService.Data;

public class CookbookDbContext(DbContextOptions<CookbookDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardPermission> BoardPermissions => Set<BoardPermission>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.Description)
                .HasMaxLength(2000);
            entity.Property(x => x.YieldServings)
                .HasMaxLength(200);
            entity.Property(x => x.PrepTime)
                .HasMaxLength(200);
            entity.Property(x => x.CookTime)
                .HasMaxLength(200);
            entity.Property(x => x.TotalTime)
                .HasMaxLength(200);
            entity.Property(x => x.CookingTemperature)
                .HasMaxLength(200);
            entity.Property(x => x.NutritionFacts)
                .HasMaxLength(2000);
            entity.Property(x => x.StorageInfo)
                .HasMaxLength(2000);

            var listConverter = new ValueConverter<List<string>, string>(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>());

            var listComparer = new ValueComparer<List<string>>(
                (left, right) => left == null && right == null || left != null && right != null && left.SequenceEqual(right),
                value => value == null
                    ? 0
                    : value.Aggregate(0, (current, item) => HashCode.Combine(current, item == null ? 0 : item.GetHashCode())),
                value => value == null ? new List<string>() : value.ToList());

            entity.Property(x => x.Ingredients)
                .HasConversion(listConverter)
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(listComparer);

            entity.Property(x => x.Quantities)
                .HasConversion(listConverter)
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(listComparer);

            entity.Property(x => x.Equipment)
                .HasConversion(listConverter)
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(listComparer);

            entity.Property(x => x.Instructions)
                .HasConversion(listConverter)
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(listComparer);
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

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(x => x.ProfilePictureUrl)
                .HasMaxLength(2048);
            entity.Property(x => x.CreatedUtc)
                .IsRequired();
            entity.Property(x => x.UpdatedUtc)
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
