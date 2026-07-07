namespace CESI_CI_CD.ApiService.Data.Entities;

public class Interest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}
