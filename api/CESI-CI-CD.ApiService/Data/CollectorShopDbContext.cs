using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Data;

public class CollectorShopDbContext(DbContextOptions<CollectorShopDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // La table Users est possédée par CESI-CI-CD.IdentityService (migrations, schéma) —
        // cette API ne fait que la lire/joindre (Listing.Seller, Favorite.User, etc.), jamais
        // n'en modifie le schéma.
        modelBuilder.Entity<User>()
            .ToTable("Users", t => t.ExcludeFromMigrations())
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

        modelBuilder.Entity<Favorite>()
            .HasIndex(f => new { f.UserId, f.ListingId })
            .IsUnique();

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Listing)
            .WithMany()
            .HasForeignKey(f => f.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .HasIndex(c => new { c.ListingId, c.BuyerId })
            .IsUnique();

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Listing)
            .WithMany()
            .HasForeignKey(c => c.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Buyer)
            .WithMany()
            .HasForeignKey(c => c.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Seller)
            .WithMany()
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Interest>()
            .HasIndex(i => new { i.UserId, i.CategoryId })
            .IsUnique();

        modelBuilder.Entity<Interest>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Interest>()
            .HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Listing)
            .WithMany()
            .HasForeignKey(n => n.ListingId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.ListingId, r.ReporterId })
            .IsUnique();

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Listing)
            .WithMany()
            .HasForeignKey(r => r.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Catégories créées par l'admin (cf. contexte métier : seul l'admin crée les catégories)
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Figurines" },
            new Category { Id = new Guid("22222222-2222-2222-2222-222222222222"), Name = "Vinyles & cassettes" },
            new Category { Id = new Guid("33333333-3333-3333-3333-333333333333"), Name = "Sneakers" }
        );
    }
}
