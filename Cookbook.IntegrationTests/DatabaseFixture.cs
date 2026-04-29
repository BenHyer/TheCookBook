using Microsoft.Data.Sqlite;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Cookbook.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly string _sqliteDatabaseName = $"cookbook_integration_tests_{Guid.NewGuid():N}";
    private readonly PostgreSqlBuilder _containerBuilder = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase($"cookbook_test_{Guid.NewGuid():N}")
        .WithUsername("test")
        .WithPassword("test")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready -U test"));

    private PostgreSqlContainer? _container;
    private SqliteConnection? _sqliteConnection;

    private bool UseSqliteFallback => _sqliteConnection is not null;

    public IntegrationTestDbContext CreateContext()
    {
        var context = UseSqliteFallback
            ? new IntegrationTestDbContext(new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseSqlite(_sqliteConnection!)
                .Options)
            : new IntegrationTestDbContext(new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseNpgsql(_container!.GetConnectionString())
                .Options);

        return context;
    }

    private async Task InitializeSchemaAsync()
    {
        await using var context = CreateContext();

        // Retry EnsureCreatedAsync a few times to handle transient container startup / connection resets
        const int maxAttempts = 8;
        Exception? lastEx = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();
                lastEx = null;
                break;
            }
            catch (Exception ex)
            {
                lastEx = ex;
                var delayMs = 500 * attempt; // linear backoff
                await Task.Delay(delayMs);
            }
        }

        if (lastEx is not null)
            throw lastEx;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _container = _containerBuilder.Build();
            await _container.StartAsync();
        }
        catch
        {
            _sqliteConnection = new SqliteConnection($"Data Source=file:{_sqliteDatabaseName}?mode=memory&cache=shared");
            await _sqliteConnection.OpenAsync();
        }

        await InitializeSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_sqliteConnection is not null)
        {
            await _sqliteConnection.CloseAsync();
            await _sqliteConnection.DisposeAsync();
        }
    }
}
