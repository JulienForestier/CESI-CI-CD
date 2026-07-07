using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Data;

public class CollectorShopDbContext(DbContextOptions<CollectorShopDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Listing>()
            .HasOne(l => l.Seller)
            .WithMany(u => u.Listings)
            .HasForeignKey(l => l.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Listing>()
            .HasOne(l => l.Category)
            .WithMany(c => c.Listings)
            .HasForeignKey(l => l.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Catégories créées par l'admin (cf. contexte métier : seul l'admin crée les catégories)
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Figurines" },
            new Category { Id = new Guid("22222222-2222-2222-2222-222222222222"), Name = "Vinyles & cassettes" },
            new Category { Id = new Guid("33333333-3333-3333-3333-333333333333"), Name = "Sneakers" }
        );
    }
}
