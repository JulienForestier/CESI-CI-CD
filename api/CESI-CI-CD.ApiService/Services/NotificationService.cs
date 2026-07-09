using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Services;

public class NotificationService
{
    public async Task NotifyInterestedUsersOfNewListingAsync(CollectorShopDbContext db, Listing listing)
    {
        var interestedUserIds = await db.Interests
            .Where(i => i.CategoryId == listing.CategoryId && i.UserId != listing.SellerId)
            .Select(i => i.UserId)
            .Distinct()
            .ToListAsync();

        if (interestedUserIds.Count == 0)
        {
            return;
        }

        var categoryName = listing.Category?.Name ?? string.Empty;
        foreach (var userId in interestedUserIds)
        {
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.NewListingMatch,
                Title = "Nouvelle annonce dans vos centres d'intérêt",
                Message = $"« {listing.Title} » vient d'être publiée dans la catégorie {categoryName}.",
                ListingId = listing.Id,
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task NotifySellerOfModerationDecisionAsync(CollectorShopDbContext db, Listing listing, bool approved)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = listing.SellerId,
            Type = approved ? NotificationType.ListingApproved : NotificationType.ListingRejected,
            Title = approved ? "Votre annonce a été validée" : "Votre annonce a été rejetée",
            Message = approved
                ? $"« {listing.Title} » est maintenant publiée sur Collector.shop."
                : $"« {listing.Title} » a été rejetée. Motif : {listing.ModerationReason}",
            ListingId = listing.Id,
        });

        await db.SaveChangesAsync();
    }

    public async Task NotifySellerOfSaleAsync(CollectorShopDbContext db, Listing listing)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = listing.SellerId,
            Type = NotificationType.ListingSold,
            Title = "Votre annonce a été vendue",
            Message = $"« {listing.Title} » a été achetée. Retrouvez le détail dans vos ventes.",
            ListingId = listing.Id,
        });

        await db.SaveChangesAsync();
    }
}
