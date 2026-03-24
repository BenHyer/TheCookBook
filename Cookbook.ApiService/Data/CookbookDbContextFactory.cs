using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cookbook.ApiService.Data;

public class CookbookDbContextFactory : IDesignTimeDbContextFactory<CookbookDbContext>
{
    public CookbookDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CookbookDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__sqldb")
            ?? "Server=localhost,1433;Database=sqldb;User Id=sa;TrustServerCertificate=True;Encrypt=False;";

        optionsBuilder.UseSqlServer(connectionString);

        return new CookbookDbContext(optionsBuilder.Options);
    }
}
