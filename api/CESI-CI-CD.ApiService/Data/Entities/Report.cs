namespace CESI_CI_CD.ApiService.Data.Entities;

public class Report
{
    public Guid Id { get; set; }
    public required string Reason { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }

    public Guid ReporterId { get; set; }
    public User? Reporter { get; set; }
}
