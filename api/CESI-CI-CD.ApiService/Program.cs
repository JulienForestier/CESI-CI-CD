using CESI_CI_CD.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<CollectorShopDbContext>("collectorshop");

var app = builder.Build();

app.MapGet("/", () => "Collector.shop API");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
