using CESI_CI_CD.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<CollectorShopDbContext>("collectorshop");

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var api = app.MapGroup("/api");
api.MapGet("/", () => "Collector.shop API");

app.Run();
