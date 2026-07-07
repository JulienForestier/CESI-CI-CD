using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Endpoints;
using CESI_CI_CD.ApiService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<CollectorShopDbContext>("collectorshop");

builder.Services.AddScoped<ListingModerationService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<NotificationService>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Key' manquante.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "collector-shop-api",
            ValidateAudience = true,
            ValidAudience = "collector-shop-app",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, "Admin")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CollectorShopDbContext>();
    db.Database.Migrate();

    if (!await db.Users.AnyAsync(u => u.IsAdmin))
    {
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@collector.shop",
            DisplayName = "Admin Collector",
            IsAdmin = true,
            PasswordHash = string.Empty,
        };
        admin.PasswordHash = hasher.HashPassword(admin, "AdminDemo1234!");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapFavoriteEndpoints();
app.MapChatEndpoints();
app.MapModerationEndpoints();
app.MapInterestEndpoints();
app.MapNotificationEndpoints();
app.MapUserEndpoints();

app.Run();

public partial class Program;
