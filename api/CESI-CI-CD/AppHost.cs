var builder = DistributedApplication.CreateBuilder(args);

var bffClientSecret = builder.AddParameter("bff-client-secret", secret: true);

var collectorShopDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("collectorshop");

var identityService = builder.AddProject<Projects.CESI_CI_CD_IdentityService>("identityservice")
    .WithReference(collectorShopDb)
    .WaitFor(collectorShopDb);

var apiService = builder.AddProject<Projects.CESI_CI_CD_ApiService>("apiservice")
    .WithReference(collectorShopDb)
    .WaitFor(collectorShopDb)
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithEnvironment("IdentityService__Authority", identityService.GetEndpoint("https"))
    .WithEnvironment("Bff__ClientSecret", bffClientSecret);

identityService
    .WithEnvironment("Bff__Origin", apiService.GetEndpoint("https"))
    .WithEnvironment("Bff__ClientSecret", bffClientSecret);

builder.AddViteApp("app", "../application/collector-shop")
    .WithReference(apiService);

builder.Build().Run();
