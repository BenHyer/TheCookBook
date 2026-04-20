using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

/// <summary>
/// Creates an isolated SQL Server / Azure SQL database for one test class and drops it on dispose.
/// Each fixture instance gets a unique database name so test classes can run in parallel.
///
/// Set SQLCONNSTR to override the server connection (without a Database= segment):
///   LocalDB (default): Server=(localdb)\mssqllocaldb;Trusted_Connection=True
///   Azure SQL example: Server=tcp:myserver.database.windows.net,1433;User ID=user;Password=pass;Encrypt=True
/// </summary>
public class DatabaseFixture : IDisposable
{
    private readonly string _dbName = $"cookbook_test_{Guid.NewGuid():N}";

    public IntegrationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseSqlServer(BuildConnectionString())
            .Options;

        var context = new IntegrationTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }

    private string BuildConnectionString()
    {
        var serverConn = Environment.GetEnvironmentVariable("SQLCONNSTR")
            ?? @"Server=(localdb)\mssqllocaldb;Trusted_Connection=True;MultipleActiveResultSets=True";

        var builder = new SqlConnectionStringBuilder(serverConn)
        {
            InitialCatalog = _dbName
        };
        return builder.ConnectionString;
    }
}
