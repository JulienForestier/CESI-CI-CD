using CESI_CI_CD.ApiService.Data.Entities;

namespace CESI_CI_CD.ApiService.Contracts;

public record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    bool IsRead,
    DateTimeOffset CreatedAt,
    Guid? ListingId);
