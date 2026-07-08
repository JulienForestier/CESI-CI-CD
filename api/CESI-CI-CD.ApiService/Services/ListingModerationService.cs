using CESI_CI_CD.ApiService.Data.Entities;

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
    public const int AutoPublishScoreThreshold = 70;
    public const decimal HighPriceThreshold = 2_000m;
    public const int ShortDescriptionThreshold = 20;

    private static readonly string[] SuspiciousKeywords =
        ["gratuit", "urgent", "arnaque", "hors plateforme", "paiement direct"];

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

    public ModerationReview Review(string title, string description, decimal price)
    {
        if (!IsApproved(title, description, price))
        {
            return new ModerationReview(ListingStatus.Rejected, 0, "Titre, description ou prix invalide.");
        }

        var score = 100;
        var reasons = new List<string>();

        if (SuspiciousKeywords.Any(keyword => title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            score -= 40;
            reasons.Add("titre suspect");
        }

        if (description.Trim().Length < ShortDescriptionThreshold)
        {
            score -= 15;
            reasons.Add("description succincte");
        }

        if (price > HighPriceThreshold)
        {
            score -= 10;
            reasons.Add("prix élevé");
        }

        score = Math.Clamp(score, 0, 100);
        var status = score >= AutoPublishScoreThreshold ? ListingStatus.Published : ListingStatus.Pending;
        var reason = reasons.Count == 0 ? "RAS" : string.Join(", ", reasons);

        return new ModerationReview(status, score, reason);
    }
}

public record ModerationReview(ListingStatus Status, int QualityScore, string Reason);
