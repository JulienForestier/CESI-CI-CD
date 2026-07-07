using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/conversations", async (
            StartConversationRequest request,
            ClaimsPrincipal user,
            CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId && l.Status == ListingStatus.Published);
            if (listing is null)
            {
                return Results.NotFound();
            }

            if (listing.SellerId == userId)
            {
                return Results.BadRequest(new { message = "Vous ne pouvez pas démarrer une conversation avec vous-même." });
            }

            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.ListingId == request.ListingId && c.BuyerId == userId);

            if (conversation is null)
            {
                conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    BuyerId = userId.Value,
                    SellerId = listing.SellerId,
                };
                db.Conversations.Add(conversation);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { conversation.Id });
        }).RequireAuthorization();

        api.MapGet("/conversations", async (ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var conversations = await db.Conversations
                .Include(c => c.Listing)
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .Include(c => c.Messages)
                .Where(c => c.BuyerId == userId || c.SellerId == userId)
                .ToListAsync();

            var responses = conversations
                .Select(c => ToConversationResponse(c, userId.Value))
                .OrderByDescending(c => c.LastMessageAt ?? DateTimeOffset.MinValue)
                .ToList();

            return Results.Ok(responses);
        }).RequireAuthorization();

        api.MapGet("/conversations/{id:guid}/messages", async (Guid id, ClaimsPrincipal user, CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var conversation = await db.Conversations
                .Include(c => c.Messages).ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation is null)
            {
                return Results.NotFound();
            }

            if (conversation.BuyerId != userId && conversation.SellerId != userId)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            if (conversation.BuyerId == userId)
            {
                conversation.BuyerLastReadAt = DateTimeOffset.UtcNow;
            }
            else
            {
                conversation.SellerLastReadAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync();

            var messages = conversation.Messages
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageResponse(m.Id, m.ConversationId, m.SenderId, m.Sender?.DisplayName ?? string.Empty, m.Body, m.SentAt))
                .ToList();

            return Results.Ok(messages);
        }).RequireAuthorization();

        api.MapPost("/conversations/{id:guid}/messages", async (
            Guid id,
            SendMessageRequest request,
            ClaimsPrincipal user,
            CollectorShopDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id);
            if (conversation is null)
            {
                return Results.NotFound();
            }

            if (conversation.BuyerId != userId && conversation.SellerId != userId)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var body = request.Body.Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                return Results.BadRequest(new { message = "Le message ne peut pas être vide." });
            }

            if (ContactInfoFilter.ContainsContactInfo(body))
            {
                return Results.BadRequest(new
                {
                    message = "Le partage de coordonnées personnelles (email, téléphone) n'est pas autorisé sur Collector.shop.",
                });
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderId = userId.Value,
                Body = body,
            };

            db.Messages.Add(message);
            await db.SaveChangesAsync();

            var sender = await db.Users.FirstAsync(u => u.Id == userId);

            return Results.Created(
                $"/api/conversations/{conversation.Id}/messages",
                new MessageResponse(message.Id, message.ConversationId, message.SenderId, sender.DisplayName, message.Body, message.SentAt));
        }).RequireAuthorization();
    }

    private static ConversationResponse ToConversationResponse(Conversation c, Guid userId)
    {
        var lastMessage = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
        var isBuyer = c.BuyerId == userId;
        var counterpart = isBuyer ? c.Seller! : c.Buyer!;
        var lastReadAt = isBuyer ? c.BuyerLastReadAt : c.SellerLastReadAt;
        var hasUnread = lastMessage is not null && lastMessage.SenderId != userId && (lastReadAt is null || lastMessage.SentAt > lastReadAt);

        return new ConversationResponse(
            c.Id,
            c.ListingId,
            c.Listing?.Title ?? string.Empty,
            counterpart.Id,
            counterpart.DisplayName,
            lastMessage?.Body,
            lastMessage?.SentAt,
            hasUnread);
    }
}
