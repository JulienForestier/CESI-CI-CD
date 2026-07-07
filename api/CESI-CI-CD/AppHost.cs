var builder = DistributedApplication.CreateBuilder(args);

var jwtKey = builder.AddParameter("jwt-key", secret: true);

var collectorShopDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("collectorshop");

var apiService = builder.AddProject<Projects.CESI_CI_CD_ApiService>("apiservice")
    .WithReference(collectorShopDb)
    .WaitFor(collectorShopDb)
    .WithEnvironment("Jwt__Key", jwtKey);

builder.AddViteApp("app", "../application/collector-shop")
    .WithReference(apiService);

builder.Build().Run();