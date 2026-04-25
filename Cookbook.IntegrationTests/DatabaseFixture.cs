using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Cookbook.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase($"cookbook_test_{Guid.NewGuid():N}")
        .WithUsername("test")
        .WithPassword("test")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready -U test"))
        .Build();

    public IntegrationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        var context = new IntegrationTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync()
    {
        using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await _container.DisposeAsync();
    }
}
