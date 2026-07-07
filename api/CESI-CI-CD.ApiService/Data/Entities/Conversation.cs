namespace CESI_CI_CD.ApiService.Data.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }

    public Guid BuyerId { get; set; }
    public User? Buyer { get; set; }

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    public DateTimeOffset? BuyerLastReadAt { get; set; }
    public DateTimeOffset? SellerLastReadAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
