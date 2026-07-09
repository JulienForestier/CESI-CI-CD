using System.Security.Claims;
using CESI_CI_CD.ApiService.Data;
using Duende.Bff;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class FavoriteEndpoints
{
    public static void MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Favorites.Base);
        api.AsBffApiEndpoint();

        api.MapGet("/favorites", GetFavoritesAsync).RequireAuthorization();
        api.MapGet("/favorites/ids", GetFavoriteIdsAsync).RequireAuthorization();
        api.MapPut("/listings/{id:guid}/favorite", AddFavoriteAsync).RequireAuthorization();
        api.MapDelete("/listings/{id:guid}/favorite", RemoveFavoriteAsync).RequireAuthorization();
    }

    private static async Task<IResult> GetFavoritesAsync(ClaimsPrincipal user, CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } userId)
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
    }

    private static async Task<IResult> GetFavoriteIdsAsync(ClaimsPrincipal user, CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var ids = await db.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.ListingId)
            .ToListAsync();

        return Results.Ok(ids);
    }

    private static async Task<IResult> AddFavoriteAsync(Guid id, ClaimsPrincipal user, CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } userId)
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
            db.Favorites.Add(new Favorite { Id = Guid.NewGuid(), UserId = userId, ListingId = id });
            await db.SaveChangesAsync();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFavoriteAsync(Guid id, ClaimsPrincipal user, CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } userId)
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
    }
}
