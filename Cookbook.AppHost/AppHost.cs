var builder = DistributedApplication.CreateBuilder(args);

var dotEnv = LoadDotEnv(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\.env")));
string? GetAzureAdValue(string configKey, string envKey)
    => builder.Configuration[configKey]
       ?? (dotEnv.TryGetValue(envKey, out var value) ? value : null);

var sqlContainer = builder.AddSqlServer("sql")
    .WithDataVolume();

var sqldb = sqlContainer.AddDatabase("sqldb");

var apiProject = builder.AddProject<Projects.Cookbook_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(sqldb)
    .WaitFor(sqldb);

builder.AddProject<Projects.CookbookMauiBlazor_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiProject)
    .WithEnvironment("AzureAd__Instance", GetAzureAdValue("AzureAd:Instance", "AZURE_AD_INSTANCE"))
    .WithEnvironment("AzureAd__TenantId", GetAzureAdValue("AzureAd:TenantId", "AZURE_AD_TENANT_ID"))
    .WithEnvironment("AzureAd__ClientId", GetAzureAdValue("AzureAd:ClientId", "AZURE_AD_CLIENT_ID"))
    .WithEnvironment("AzureAd__ClientSecret", GetAzureAdValue("AzureAd:ClientSecret", "AZURE_AD_CLIENT_SECRET"))
    .WaitFor(apiProject);

builder.Build().Run();

static Dictionary<string, string> LoadDotEnv(string filePath)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    if (!File.Exists(filePath))
    {
        return values;
    }

    foreach (var rawLine in File.ReadAllLines(filePath))
    {
        var line = rawLine.Trim();

        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var equalsIndex = line.IndexOf('=');
        if (equalsIndex <= 0)
        {
            continue;
        }

        var key = line[..equalsIndex].Trim();
        var value = line[(equalsIndex + 1)..].Trim();

        if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        values[key] = value;
    }

    return values;
}
