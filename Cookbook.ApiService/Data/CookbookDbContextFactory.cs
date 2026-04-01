using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cookbook.ApiService.Data;

public class CookbookDbContextFactory : IDesignTimeDbContextFactory<CookbookDbContext>
{
    public CookbookDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddUserSecrets<CookbookDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("sqldb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'sqldb' is not configured for design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<CookbookDbContext>();

        optionsBuilder.UseSqlServer(connectionString);

        return new CookbookDbContext(optionsBuilder.Options);
    }
}
