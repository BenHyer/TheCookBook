var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume();

var db = sql.AddDatabase("sqldb");

var apiService = builder.AddProject<Projects.Cookbook_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db);

builder.AddProject<Projects.Cookbook_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.Cookbook_Maui>("mauiapp")
    .WithReference(apiService);

builder.Build().Run();
