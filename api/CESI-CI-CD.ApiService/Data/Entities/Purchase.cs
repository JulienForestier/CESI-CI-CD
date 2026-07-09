namespace CESI_CI_CD.ApiService.Data.Entities;

public class Purchase
{
    public Guid Id { get; set; }

    // Prix et commission capturés au moment de l'achat (indépendants d'une éventuelle
    // modification ultérieure de l'annonce) — historique de transaction immuable.
    public decimal Price { get; set; }
    public decimal CommissionAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }

    public Guid BuyerId { get; set; }
    public User? Buyer { get; set; }

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }
}
