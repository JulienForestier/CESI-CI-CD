using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CESI_CI_CD.IdentityService.Data;

/// <summary>
/// Permet à `dotnet ef migrations` de construire le DbContext sans démarrer toute l'application
/// (Program.cs exige des variables de configuration — Bff:Origin, etc. — absentes à ce stade).
/// Non utilisée à l'exécution normale.
/// </summary>
public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        // NOSONAR (csharpsquid:S2068) : identifiants de dev locaux non sensibles, utilisés
        // uniquement par l'outillage `dotnet ef` en design-time — jamais à l'exécution.
        optionsBuilder.UseNpgsql("Host=localhost;Database=collectorshop;Username=postgres;Password=postgres");
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
