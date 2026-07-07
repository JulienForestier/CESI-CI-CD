namespace CESI_CI_CD.ApiService.Services;

/// <summary>
/// Contrôle automatisé basique appliqué à chaque annonce avant mise en ligne,
/// pour limiter l'intervention humaine (cf. contexte métier Collector.shop).
/// </summary>
public class ListingModerationService
{
    public const int MinTitleLength = 3;
    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 100_000m;

    public bool IsApproved(string title, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < MinTitleLength)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        if (price < MinPrice || price > MaxPrice)
        {
            return false;
        }

        return true;
    }
}
