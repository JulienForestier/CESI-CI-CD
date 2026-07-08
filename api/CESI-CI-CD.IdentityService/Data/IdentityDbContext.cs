using CESI_CI_CD.IdentityService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.IdentityService.Data;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .ToTable("Users")
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
