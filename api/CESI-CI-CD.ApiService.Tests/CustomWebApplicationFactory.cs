using CESI_CI_CD.ApiService.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CESI_CI_CD.ApiService.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public readonly string DatabaseName = Guid.NewGuid().ToString();

    // Duende.BFF exige un header statique "X-CSRF: 1" sur toute requête vers un endpoint
    // .AsBffApiEndpoint(), authentifié ou non (protection anti-CSRF indépendante de l'auth) —
    // voir BffAntiForgeryMiddleware. Le vrai frontend l'envoie déjà (api/client.ts) ; ici on
    // l'ajoute par défaut sur chaque HttpClient de test pour ne pas le répéter partout.
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-CSRF", "1");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CollectorShopDbContext>().Database.EnsureCreated();

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:collectorshop"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["IdentityService:Authority"] = "https://localhost:5100",
                ["Bff:Origin"] = "https://localhost:5050",
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(CollectorShopDbContext)
                    || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(CollectorShopDbContext))))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<CollectorShopDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));

            // Remplace le scheme cookie/Duende.BFF réel (qui suppose un IdentityService
            // joignable) par un scheme de test piloté par en-tête — voir TestAuthHandler.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}
