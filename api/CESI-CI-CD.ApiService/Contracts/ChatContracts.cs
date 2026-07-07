namespace CESI_CI_CD.ApiService.Contracts;

public record StartConversationRequest(Guid ListingId);

public record SendMessageRequest(string Body);

public record ConversationResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    Guid CounterpartId,
    string CounterpartDisplayName,
    string? LastMessageBody,
    DateTimeOffset? LastMessageAt,
    bool HasUnread);

public record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset SentAt);
