using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Endpoints;
using CESI_CI_CD.ApiService.Services;
using Duende.Bff;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<CollectorShopDbContext>("collectorshop");

builder.Services.AddScoped<ListingModerationService>();
builder.Services.AddScoped<NotificationService>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddBff(options =>
{
    options.LicenseKey = builder.Configuration["Duende:Bff:LicenseKey"];
})
    .ConfigureOpenIdConnect(options =>
    {
        // Lecture de config différée jusqu'ici (pas en variable top-level) : WebApplicationFactory
        // (tests d'intégration) injecte sa configuration après l'exécution des instructions
        // top-level de Program.cs, donc une lecture immédiate de builder.Configuration échouerait.
        var bffOrigin = builder.Configuration["Bff:Origin"]
            ?? throw new InvalidOperationException("Configuration 'Bff:Origin' manquante.");

        options.Authority = builder.Configuration["IdentityService:Authority"]
            ?? throw new InvalidOperationException("Configuration 'IdentityService:Authority' manquante.");
        // Autorise une Authority en http:// en développement local (vérification manuelle sans
        // certificats) — en dehors de Development, une Authority http déclenchera l'erreur
        // IDX20803 par défaut, ce qui est le comportement voulu en production.
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.ClientId = "collector-shop-bff";
        options.ClientSecret = builder.Configuration["Bff:ClientSecret"] ?? "dev-only-secret-not-for-production";
        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Scope.Add("roles");
        options.Scope.Add("offline_access");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "role",
            NameClaimType = "name",
        };

        // Sans ceci, le handler OIDC déduit le redirect_uri du Host de la requête entrante —
        // ce qui diverge de "Bff:Origin" (utilisé par IdentityService pour enregistrer le
        // client) dès qu'un proxy intermédiaire réécrit le Host (proxy Vite en dev local,
        // ingress avec réécriture en prod). On fixe explicitement les deux URIs de callback
        // pour qu'elles correspondent toujours à ce qui est enregistré côté client OIDC.
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.RedirectUri = $"{bffOrigin}/signin-oidc";
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToIdentityProviderForSignOut = context =>
        {
            context.ProtocolMessage.PostLogoutRedirectUri = $"{bffOrigin}/signout-callback-oidc";
            return Task.CompletedTask;
        };
    })
    .ConfigureCookies(options =>
    {
        options.Cookie.Name = "collector-shop-session";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("role", "Admin")));

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? throw new InvalidOperationException("Configuration 'Cors:AllowedOrigins' manquante.");

    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CollectorShopDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseAuthentication();
app.UseRouting();
app.UseBff();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapCatalogEndpoints();
app.MapFavoriteEndpoints();
app.MapChatEndpoints();
app.MapModerationEndpoints();
app.MapInterestEndpoints();
app.MapNotificationEndpoints();
app.MapUserEndpoints();

app.Run();

public partial class Program;
