using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/categories", async (CollectorShopDbContext db) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse(c.Id, c.Name))
                .ToListAsync();

            return Results.Ok(categories);
        });

        api.MapGet("/listings", async (CollectorShopDbContext db, Guid? categoryId) =>
        {
            var query = db.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Where(l => l.Status == ListingStatus.Published);

            if (categoryId is not null)
            {
                query = query.Where(l => l.CategoryId == categoryId);
            }

            var listings = await query
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ToResponse(l))
                .ToListAsync();

            return Results.Ok(listings);
        });

        api.MapGet("/listings/{id:guid}", async (Guid id, CollectorShopDbContext db) =>
        {
            var listing = await db.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Where(l => l.Id == id && l.Status == ListingStatus.Published)
                .Select(l => ToResponse(l))
                .FirstOrDefaultAsync();

            return listing is null ? Results.NotFound() : Results.Ok(listing);
        });

        api.MapPost("/listings", async (
            CreateListingRequest request,
            ClaimsPrincipal user,
            CollectorShopDbContext db,
            ListingModerationService moderationService) =>
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

            var isApproved = moderationService.IsApproved(request.Title, request.Description, request.Price);

            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Status = isApproved ? ListingStatus.Published : ListingStatus.Rejected,
                SellerId = sellerId.Value,
                CategoryId = request.CategoryId,
            };

            db.Listings.Add(listing);
            await db.SaveChangesAsync();

            await db.Entry(listing).Reference(l => l.Seller).LoadAsync();
            await db.Entry(listing).Reference(l => l.Category).LoadAsync();

            return Results.Created($"/api/listings/{listing.Id}", ToResponse(listing));
        }).RequireAuthorization();
    }

    private static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static ListingResponse ToResponse(Listing listing) => new(
        listing.Id,
        listing.Title,
        listing.Description,
        listing.Price,
        listing.Status,
        listing.CreatedAt,
        listing.SellerId,
        listing.Seller?.DisplayName ?? string.Empty,
        listing.CategoryId,
        listing.Category?.Name ?? string.Empty);
}
