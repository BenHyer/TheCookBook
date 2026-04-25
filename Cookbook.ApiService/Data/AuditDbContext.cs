using Cookbook.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cookbook.ApiService.Data;

public class AuditDbContext(DbContextOptions<AuditDbContext> options, IConfiguration configuration) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = configuration["AuditDb:Schema"] ?? "public";
        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.Entity<AuditLogEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(200);
            e.Property(x => x.UserId).HasMaxLength(200);
            e.Property(x => x.Details).HasMaxLength(2000);
            e.HasIndex(x => x.TimestampUtc);
            e.HasIndex(x => x.UserId);
        });
    }
}
