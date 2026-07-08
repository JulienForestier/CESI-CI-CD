using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CESI_CI_CD.IdentityService;
using CESI_CI_CD.IdentityService.Data;
using CESI_CI_CD.IdentityService.Data.Entities;
using CESI_CI_CD.IdentityService.Endpoints;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<IdentityDbContext>("collectorshop");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var bffOrigin = builder.Configuration["Bff:Origin"]
    ?? throw new InvalidOperationException("Configuration 'Bff:Origin' manquante.");
var bffClientSecret = builder.Configuration["Bff:ClientSecret"] ?? "dev-only-secret-not-for-production";

builder.Services.AddIdentityServer(options =>
{
    options.LicenseKey = builder.Configuration["Duende:IdentityServer:LicenseKey"];
    options.UserInteraction.LoginUrl = "/login";
    options.UserInteraction.LoginReturnUrlParameter = "returnUrl";

    // Duende active par défaut l'Automatic Key Management, qui tente d'écrire des clés générées
    // sous ./keys — inutile ici puisqu'on fournit déjà un certificat de signature explicite
    // (LoadOrCreateSigningCertificate ci-dessous), et surtout incompatible avec le conteneur qui
    // tourne en utilisateur non-root sans accès en écriture à /app (échec en 500 sur TOUS les
    // endpoints, y compris /.well-known/openid-configuration, confirmé en testant l'image buildée).
    options.KeyManagement.Enabled = false;

    // En k8s, ApiService atteint ce service par son URL interne au cluster (rapide, pas de saut
    // par l'ingress public) pour le back-channel (PAR/token/userinfo/discovery), alors que le
    // navigateur y arrive par l'URL publique de l'ingress pour les pages login/register — sans
    // ceci, l'issuer auto-détecté (basé sur le Host de la requête) diffèrerait selon le chemin
    // emprunté et ferait échouer la validation "invalid issuer" côté ApiService. En local
    // (aucune config fournie), l'auto-détection reste inchangée — les deux services partagent
    // déjà la même origine dans ce cas.
    var issuerUri = builder.Configuration["IdentityServer:IssuerUri"];
    if (!string.IsNullOrEmpty(issuerUri))
    {
        options.IssuerUri = issuerUri;
    }
})
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryClients(Config.GetClients(bffOrigin, bffClientSecret))
    .AddProfileService<CustomProfileService>()
    .AddSigningCredential(LoadOrCreateSigningCertificate(builder.Configuration), SecurityAlgorithms.RsaSha256);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        // Une limite réaliste (10/5min) ferait échouer les tests d'intégration, qui partagent
        // tous le même hôte (et donc le même compteur) au sein d'une classe de test.
        opt.PermitLimit = builder.Environment.IsEnvironment("Testing") ? 1000 : 10;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.MigrateAsync();
    }

    if (!app.Environment.IsEnvironment("Testing") && !await db.Users.AnyAsync(u => u.IsAdmin))
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
        var adminPassword = builder.Configuration["Admin:Password"] ?? "AdminDemo1234!";
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}

app.UseStaticFiles();
// identity-ui est buildé avec base "/identity-assets/" (évite la collision avec les assets
// hashés de collector-shop sous /assets/* du même host d'ingress) — ses fichiers restent
// physiquement sous wwwroot/assets, donc on les republie sous ce préfixe d'URL. On construit le
// chemin depuis ContentRootPath (toujours renseigné) plutôt que WebRootPath, qui reste null tant
// que wwwroot n'existe pas encore sur disque (ex. avant le build de identity-ui, ou en test) —
// PhysicalFileProvider lève une exception à la construction si le dossier n'existe pas, donc on
// le crée au besoin (dossier vide : sert simplement des 404 tant que identity-ui n'est pas buildé).
var identityAssetsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "assets");
Directory.CreateDirectory(identityAssetsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(identityAssetsPath),
    RequestPath = "/identity-assets/assets",
});
app.UseRouting();
app.UseIdentityServer();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapAuthEndpoints();
app.MapFallbackToFile("index.html");

await app.RunAsync();

static X509Certificate2 LoadOrCreateSigningCertificate(IConfiguration configuration)
{
    var pfxBase64 = configuration["IdentityServer:SigningCertificate"];
    var pfxPassword = configuration["IdentityServer:SigningCertificatePassword"];
    if (pfxBase64 is not null && pfxPassword is not null)
    {
        return X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(pfxBase64), pfxPassword);
    }

    // Développement local / tests : certificat auto-signé éphémère, régénéré à chaque démarrage.
    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest(
        "CN=collector-shop-identity-dev",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);
    return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(1));
}
