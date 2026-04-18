using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

/// <summary>
/// Shared fixture that creates the schema once per test class and drops it on dispose.
/// Even if tests fail, Dispose() is called by xunit — so no artifacts are left.
/// </summary>
public class DatabaseFixture : IDisposable
{
    public IntegrationTestDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("PGCONNSTR")
            ?? "Host=localhost;Database=cookbook_integration_test;Username=postgres;Password=testpass";

        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new IntegrationTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        // Drop the schema so the next run starts clean.
        // This is also called by xunit when a test class fails partway through.
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}
