using System.Security.Claims;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class FavoriteEndpoints
{
    public static void MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Favorites.Base);

        api.MapGet("/favorites", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var favorites = await db.Favorites
                .Include(f => f.Listing!).ThenInclude(l => l.Seller)
                .Include(f => f.Listing!).ThenInclude(l => l.Category)
                .Where(f => f.UserId == userId && f.Listing!.Status == ListingStatus.Published)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Results.Ok(favorites.Select(f => ListingMapper.ToResponse(f.Listing!)));
        }).RequireAuthorization();

        api.MapGet("/favorites/ids", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var ids = await db.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ListingId)
                .ToListAsync();

            return Results.Ok(ids);
        }).RequireAuthorization();

        api.MapPut("/listings/{id:guid}/favorite", async (Guid id, ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var listingExists = await db.Listings.AnyAsync(l => l.Id == id);
            if (!listingExists)
            {
                return Results.NotFound();
            }

            var alreadyFavorited = await db.Favorites.AnyAsync(f => f.UserId == userId && f.ListingId == id);
            if (!alreadyFavorited)
            {
                db.Favorites.Add(new Favorite { Id = Guid.NewGuid(), UserId = userId.Value, ListingId = id });
                await db.SaveChangesAsync();
            }

            return Results.NoContent();
        }).RequireAuthorization();

        api.MapDelete("/listings/{id:guid}/favorite", async (Guid id, ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == id);
            if (favorite is not null)
            {
                db.Favorites.Remove(favorite);
                await db.SaveChangesAsync();
            }

            return Results.NoContent();
        }).RequireAuthorization();
    }
}
