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

        api.MapGet("", GetNotificationsAsync);
        api.MapPost("/mark-all-read", MarkAllReadAsync);
    }

    private static async Task<IResult> GetNotificationsAsync(ClaimsPrincipal user, CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var notifications = await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse(n.Id, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt, n.ListingId))
            .ToListAsync();

        return Results.Ok(notifications);
    }

    private static async Task<IResult> MarkAllReadAsync(ClaimsPrincipal user, CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } userId)
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
    }
}
