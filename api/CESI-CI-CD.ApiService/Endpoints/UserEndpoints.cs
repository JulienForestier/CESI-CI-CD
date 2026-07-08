using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Users.Base).RequireAuthorization();

        api.MapGet("/me", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var entity = await db.Users.FindAsync(userId);
            if (entity is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new UserProfileResponse(entity.Id, entity.Email, entity.DisplayName, entity.IsAdmin, entity.CreatedAt));
        });

        api.MapPatch("/me", async (UpdateProfileRequest request, ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Results.BadRequest(new { message = "Le pseudo ne peut pas être vide." });
            }

            var entity = await db.Users.FindAsync(userId);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.DisplayName = request.DisplayName.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new UserProfileResponse(entity.Id, entity.Email, entity.DisplayName, entity.IsAdmin, entity.CreatedAt));
        });
    }
}
