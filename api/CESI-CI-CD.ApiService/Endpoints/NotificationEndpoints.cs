using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using Duende.Bff;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Notifications.Base).RequireAuthorization();
        api.AsBffApiEndpoint();

        api.MapGet("", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var notifications = await db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationResponse(n.Id, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt, n.ListingId))
                .ToListAsync();

            return Results.Ok(notifications);
        });

        api.MapPost("/mark-all-read", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var unread = await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
