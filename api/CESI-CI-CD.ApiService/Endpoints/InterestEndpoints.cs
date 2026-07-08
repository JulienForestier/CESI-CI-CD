using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class InterestEndpoints
{
    public static void MapInterestEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Interests.Base).RequireAuthorization();

        api.MapGet("", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var categoryIds = await db.Interests
                .Where(i => i.UserId == userId)
                .Select(i => i.CategoryId)
                .ToListAsync();

            return Results.Ok(categoryIds);
        });

        api.MapPut("", async (UpdateInterestsRequest request, ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var distinctCategoryIds = request.CategoryIds.Distinct().ToList();
            var validCategoryCount = await db.Categories.CountAsync(c => distinctCategoryIds.Contains(c.Id));
            if (validCategoryCount != distinctCategoryIds.Count)
            {
                return Results.BadRequest(new { message = "Une ou plusieurs catégories sont inconnues." });
            }

            var existing = await db.Interests.Where(i => i.UserId == userId).ToListAsync();
            db.Interests.RemoveRange(existing);

            foreach (var categoryId in distinctCategoryIds)
            {
                db.Interests.Add(new Interest { Id = Guid.NewGuid(), UserId = userId.Value, CategoryId = categoryId });
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
