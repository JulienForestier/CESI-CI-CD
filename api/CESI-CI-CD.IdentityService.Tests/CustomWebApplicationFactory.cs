using CESI_CI_CD.IdentityService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CESI_CI_CD.IdentityService.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public readonly string DatabaseName = Guid.NewGuid().ToString();

    static CustomWebApplicationFactory()
    {
        // Program.cs lit Bff:Origin en instruction top-level (nécessaire pour construire
        // Config.GetClients(...) avant .Build()) — WebApplicationFactory.ConfigureAppConfiguration
        // n'injecte sa config qu'après l'exécution de ces instructions top-level, donc une
        // collection en mémoire arriverait trop tard. Les variables d'environnement, elles, sont
        // déjà visibles au moment de WebApplication.CreateBuilder(args) (AddEnvironmentVariables
        // s'exécute en synchrone dès la construction du builder).
        Environment.SetEnvironmentVariable("Bff__Origin", "https://localhost:5050");
        Environment.SetEnvironmentVariable("Bff__ClientSecret", "test-client-secret");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.EnsureCreated();

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
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(IdentityDbContext)
                    || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(IdentityDbContext))))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));
        });
    }
}
