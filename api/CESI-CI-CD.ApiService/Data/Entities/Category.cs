namespace CESI_CI_CD.ApiService.Data.Entities;

public class Category
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
}
