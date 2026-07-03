var builder = DistributedApplication.CreateBuilder(args);

builder.AddViteApp("app", "../application/collector-shop");
builder.Build().Run();