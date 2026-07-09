using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using Duende.Bff;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class ModerationEndpoints
{
    public static void MapModerationEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Moderation.Base).RequireAuthorization("AdminOnly");
        api.AsBffApiEndpoint();

        api.MapGet("/listings/pending", GetPendingListingsAsync);
        api.MapPost("/listings/{id:guid}/approve", ApproveListingAsync);
        api.MapPost("/listings/{id:guid}/reject", RejectListingAsync);
    }

    private static async Task<IResult> GetPendingListingsAsync(CollectorShopDbContext db)
    {
        var listings = await db.Listings
            .Include(l => l.Seller)
            .Include(l => l.Category)
            .Where(l => l.Status == ListingStatus.Pending)
            .OrderBy(l => l.CreatedAt)
            .Select(l => ListingMapper.ToResponse(l))
            .ToListAsync();

        return Results.Ok(listings);
    }

    private static async Task<IResult> ApproveListingAsync(Guid id, CollectorShopDbContext db, NotificationService notificationService)
    {
        var listing = await db.Listings
            .Include(l => l.Seller)
            .Include(l => l.Category)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
        {
            return Results.NotFound();
        }

        if (listing.Status != ListingStatus.Pending)
        {
            return Results.BadRequest(new { message = "Seule une annonce en attente peut être validée." });
        }

        listing.Status = ListingStatus.Published;
        await db.SaveChangesAsync();

        await notificationService.NotifySellerOfModerationDecisionAsync(db, listing, approved: true);
        await notificationService.NotifyInterestedUsersOfNewListingAsync(db, listing);

        return Results.Ok(ListingMapper.ToResponse(listing));
    }

    private static async Task<IResult> RejectListingAsync(
        Guid id,
        RejectListingRequest request,
        CollectorShopDbContext db,
        NotificationService notificationService)
    {
        var listing = await db.Listings
            .Include(l => l.Seller)
            .Include(l => l.Category)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
        {
            return Results.NotFound();
        }

        if (listing.Status != ListingStatus.Pending)
        {
            return Results.BadRequest(new { message = "Seule une annonce en attente peut être rejetée." });
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.BadRequest(new { message = "Un motif de rejet est requis." });
        }

        listing.Status = ListingStatus.Rejected;
        listing.ModerationReason = request.Reason.Trim();
        await db.SaveChangesAsync();

        await notificationService.NotifySellerOfModerationDecisionAsync(db, listing, approved: false);

        return Results.Ok(ListingMapper.ToResponse(listing));
    }
}
