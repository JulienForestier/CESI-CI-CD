using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data.Entities;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class ListingMapper
{
    public static ListingResponse ToResponse(Listing listing) => new(
        listing.Id,
        listing.Title,
        listing.Description,
        listing.Price,
        listing.Status,
        listing.QualityScore,
        listing.ModerationReason,
        listing.CreatedAt,
        listing.SellerId,
        listing.Seller?.DisplayName ?? string.Empty,
        listing.CategoryId,
        listing.Category?.Name ?? string.Empty);
}
