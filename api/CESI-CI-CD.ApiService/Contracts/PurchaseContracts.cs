namespace CESI_CI_CD.ApiService.Contracts;

public record PurchaseResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    Guid BuyerId,
    Guid SellerId,
    string SellerDisplayName,
    decimal Price,
    decimal CommissionAmount,
    DateTimeOffset CreatedAt);
