namespace CESI_CI_CD.ApiService.Data.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? ListingId { get; set; }
    public Listing? Listing { get; set; }
}
