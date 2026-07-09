using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Services;
using Duende.Bff;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class PurchaseEndpoints
{
    private const decimal CommissionRate = 0.05m;

    public static void MapPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Catalog.Base).RequireAuthorization();
        api.AsBffApiEndpoint();

        api.MapPost("/listings/{id:guid}/purchase", PurchaseListingAsync);
        api.MapGet("/purchases/mine", GetMyPurchasesAsync);
    }

    private static async Task<IResult> PurchaseListingAsync(
        Guid id,
        ClaimsPrincipal user,
        CollectorShopDbContext db,
        NotificationService notificationService)
    {
        if (user.GetUserId() is not { } buyerId)
        {
            return Results.Unauthorized();
        }

        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null)
        {
            return Results.NotFound();
        }

        if (listing.SellerId == buyerId)
        {
            return Results.BadRequest(new { message = "Vous ne pouvez pas acheter votre propre annonce." });
        }

        if (listing.Status != ListingStatus.Published)
        {
            return Results.Conflict(new { message = "Cette annonce n'est plus disponible à l'achat." });
        }

        listing.Status = ListingStatus.Sold;
        listing.ConcurrencyStamp = Guid.NewGuid();

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            BuyerId = buyerId,
            SellerId = listing.SellerId,
            Price = listing.Price,
            CommissionAmount = Math.Round(listing.Price * CommissionRate, 2),
        };
        db.Purchases.Add(purchase);

        // Concurrence optimiste (ConcurrencyStamp, voir CollectorShopDbContext) : si un autre
        // acheteur a déjà modifié cette même ligne entre notre lecture et cet enregistrement — deux
        // acheteurs qui cliquent au même instant sur la même annonce —, EF Core détecte que le
        // jeton lu ne correspond plus à celui en base et lève DbUpdateConcurrencyException. Un
        // seul des deux appels concurrents réussit (transition d'état + création de l'achat dans
        // la même transaction) ; l'autre reçoit 409 Conflict sans qu'aucun achat ne soit créé. Pas
        // de verrou explicite (SELECT ... FOR UPDATE) ni de transaction manuelle nécessaire.
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "Cette annonce n'est plus disponible à l'achat." });
        }

        await notificationService.NotifySellerOfSaleAsync(db, listing);

        var seller = await db.Users.FirstAsync(u => u.Id == listing.SellerId);

        return Results.Created(
            "/api/purchases/mine",
            ToResponse(purchase, listing.Title, seller.DisplayName));
    }

    private static async Task<IResult> GetMyPurchasesAsync(ClaimsPrincipal user, CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } buyerId)
        {
            return Results.Unauthorized();
        }

        var purchases = await db.Purchases
            .Include(p => p.Listing)
            .Include(p => p.Seller)
            .Where(p => p.BuyerId == buyerId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => ToResponse(p, p.Listing!.Title, p.Seller!.DisplayName))
            .ToListAsync();

        return Results.Ok(purchases);
    }

    private static PurchaseResponse ToResponse(Purchase purchase, string listingTitle, string sellerDisplayName) => new(
        purchase.Id,
        purchase.ListingId,
        listingTitle,
        purchase.BuyerId,
        purchase.SellerId,
        sellerDisplayName,
        purchase.Price,
        purchase.CommissionAmount,
        purchase.CreatedAt);
}
