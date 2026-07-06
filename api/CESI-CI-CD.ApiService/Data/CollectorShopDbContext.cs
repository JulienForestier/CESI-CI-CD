using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Data;

public class CollectorShopDbContext(DbContextOptions<CollectorShopDbContext> options) : DbContext(options)
{
}
