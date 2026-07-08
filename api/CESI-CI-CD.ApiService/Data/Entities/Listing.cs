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
}
