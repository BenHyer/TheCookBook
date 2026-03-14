var builder = DistributedApplication.CreateBuilder(args);

var sqlContainer = builder.AddSqlServer("sql")
    .WithDataVolume();

var sqldb = sqlContainer.AddDatabase("sqldb");

var apiProject = builder.AddProject<Projects.Cookbook_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(sqldb)
    .WaitFor(sqldb);

builder.AddProject<Projects.Cookbook_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiProject)
    .WaitFor(apiProject);

builder.AddProject<Projects.Cookbook_Maui>("mauiapp")
    .WithReference(apiProject);

builder.Build().Run();
