using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using Duende.Bff;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Catalog.Base);
        api.AsBffApiEndpoint();

        api.MapGet("/categories", async (CollectorShopDbContext db) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse(c.Id, c.Name))
                .ToListAsync();

            return Results.Ok(categories);
        });

        api.MapGet("/listings", async (CollectorShopDbContext db, Guid? categoryId, string? search) =>
        {
            var query = db.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Where(l => l.Status == ListingStatus.Published);

            if (categoryId is not null)
            {
                query = query.Where(l => l.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = search.Trim().ToLower();
                query = query.Where(l => l.Title.ToLower().Contains(pattern));
            }

            var listings = await query
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ListingMapper.ToResponse(l))
                .ToListAsync();

            return Results.Ok(listings);
        });

        api.MapGet("/listings/mine", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var sellerId = user.GetUserId();
            if (sellerId is null)
            {
                return Results.Unauthorized();
            }

            var listings = await db.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Where(l => l.SellerId == sellerId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ListingMapper.ToResponse(l))
                .ToListAsync();

            return Results.Ok(listings);
        }).RequireAuthorization();

        api.MapGet("/listings/{id:guid}", async (Guid id, CollectorShopDbContext db) =>
        {
            var listing = await db.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Where(l => l.Id == id && l.Status == ListingStatus.Published)
                .Select(l => ListingMapper.ToResponse(l))
                .FirstOrDefaultAsync();

            return listing is null ? Results.NotFound() : Results.Ok(listing);
        });

        api.MapPost("/listings", async (
            CreateListingRequest request,
            ClaimsPrincipal user,
            CollectorShopDbContext db,
            ListingModerationService moderationService,
            NotificationService notificationService) =>
        {
            var sellerId = user.GetUserId();
            if (sellerId is null)
            {
                return Results.Unauthorized();
            }

            var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
            {
                return Results.BadRequest(new { message = "Catégorie inconnue." });
            }

            var review = moderationService.Review(request.Title, request.Description, request.Price);

            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Status = review.Status,
                QualityScore = review.QualityScore,
                ModerationReason = review.Reason,
                SellerId = sellerId.Value,
                CategoryId = request.CategoryId,
            };

            db.Listings.Add(listing);
            await db.SaveChangesAsync();

            await db.Entry(listing).Reference(l => l.Seller).LoadAsync();
            await db.Entry(listing).Reference(l => l.Category).LoadAsync();

            if (listing.Status == ListingStatus.Published)
            {
                await notificationService.NotifyInterestedUsersOfNewListingAsync(db, listing);
            }

            return Results.Created($"/api/listings/{listing.Id}", ListingMapper.ToResponse(listing));
        }).RequireAuthorization();
    }
}
