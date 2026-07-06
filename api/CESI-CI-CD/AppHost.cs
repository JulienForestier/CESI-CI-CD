var builder = DistributedApplication.CreateBuilder(args);

var collectorShopDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("collectorshop");

var apiService = builder.AddProject<Projects.CESI_CI_CD_ApiService>("apiservice")
    .WithReference(collectorShopDb)
    .WaitFor(collectorShopDb);

builder.AddViteApp("app", "../application/collector-shop")
    .WithReference(apiService);

builder.Build().Run();