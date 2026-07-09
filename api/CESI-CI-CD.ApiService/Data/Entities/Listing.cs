namespace CESI_CI_CD.ApiService.Data.Entities;

public class Listing
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public ListingStatus Status { get; set; }
    public int QualityScore { get; set; }
    public required string ModerationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    // Jeton de concurrence optimiste (voir CollectorShopDbContext) — régénéré explicitement par
    // le code applicatif à chaque écriture concurrente-sensible (achat), plutôt que par un
    // mécanisme auto-généré par la base : ce dernier fonctionne sur un vrai Postgres mais n'est
    // pas fiablement simulé par le provider EF Core InMemory utilisé dans les tests d'intégration
    // (valeur jamais réellement régénérée). Un jeton géré en code se comporte identiquement sur
    // les deux, garantissant un test de concurrence représentatif du comportement en production.
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}
