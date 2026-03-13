var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Cookbook_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Cookbook_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.Cookbook_Maui>("mauiapp")
    .WithReference(apiService);

builder.Build().Run();
