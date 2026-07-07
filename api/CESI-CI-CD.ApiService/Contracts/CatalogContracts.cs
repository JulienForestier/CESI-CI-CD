using CESI_CI_CD.ApiService.Data.Entities;

namespace CESI_CI_CD.ApiService.Contracts;

public record CategoryResponse(Guid Id, string Name);

public record CreateListingRequest(string Title, string Description, decimal Price, Guid CategoryId);

public record ListingResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    ListingStatus Status,
    int QualityScore,
    string ModerationReason,
    DateTimeOffset CreatedAt,
    Guid SellerId,
    string SellerDisplayName,
    Guid CategoryId,
    string CategoryName);
