using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Services;

namespace CESI_CI_CD.ApiService.Tests.Services;

public class ListingModerationServiceTests
{
    private readonly ListingModerationService _sut = new();

    [Theory]
    [InlineData("Figurine Goku", "Très bon état, boîte incluse", 25.50)]
    [InlineData("Vinyle", "Édition originale de 1975", 0.01)]
    [InlineData("Sneakers Air", "Jamais portées", 100_000)]
    public void IsApproved_ReturnsTrue_ForValidListing(string title, string description, double price)
    {
        Assert.True(_sut.IsApproved(title, description, (decimal)price));
    }

    [Theory]
    [InlineData("", "Description valide", 10)]
    [InlineData("  ", "Description valide", 10)]
    [InlineData("ab", "Description valide", 10)]
    public void IsApproved_ReturnsFalse_WhenTitleInvalid(string title, string description, double price)
    {
        Assert.False(_sut.IsApproved(title, description, (decimal)price));
    }

    [Theory]
    [InlineData("Titre valide", "", 10)]
    [InlineData("Titre valide", "   ", 10)]
    public void IsApproved_ReturnsFalse_WhenDescriptionInvalid(string title, string description, double price)
    {
        Assert.False(_sut.IsApproved(title, description, (decimal)price));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100_000.01)]
    public void IsApproved_ReturnsFalse_WhenPriceOutOfRange(double price)
    {
        Assert.False(_sut.IsApproved("Titre valide", "Description valide", (decimal)price));
    }

    [Fact]
    public void Review_PublishesAutomatically_WhenListingIsClean()
    {
        var review = _sut.Review("Figurine Goku", "Très bon état, boîte incluse et facture", 25.50m);

        Assert.Equal(ListingStatus.Published, review.Status);
        Assert.Equal(100, review.QualityScore);
        Assert.Equal("RAS", review.Reason);
    }

    [Fact]
    public void Review_ReturnsRejected_WhenStructurallyInvalid()
    {
        var review = _sut.Review("", "Description valide", 10);

        Assert.Equal(ListingStatus.Rejected, review.Status);
        Assert.Equal(0, review.QualityScore);
    }

    [Fact]
    public void Review_FlagsForManualReview_WhenTitleIsSuspicious()
    {
        var review = _sut.Review("Vente urgente collection", "Description tout à fait normale et détaillée", 25);

        Assert.Equal(ListingStatus.Pending, review.Status);
        Assert.Equal(60, review.QualityScore);
        Assert.Contains("titre suspect", review.Reason);
    }

    [Fact]
    public void Review_FlagsForManualReview_WhenDescriptionTooShort()
    {
        var review = _sut.Review("Figurine rare", "RAS", 25);

        Assert.Equal(85, review.QualityScore);
        Assert.Equal(ListingStatus.Published, review.Status);
        Assert.Contains("description succincte", review.Reason);
    }

    [Fact]
    public void Review_FlagsForManualReview_WhenPriceIsHigh()
    {
        var review = _sut.Review("Figurine rare édition limitée", "Description détaillée et complète de l'objet", 2500);

        Assert.Equal(90, review.QualityScore);
        Assert.Equal(ListingStatus.Published, review.Status);
        Assert.Contains("prix élevé", review.Reason);
    }

    [Fact]
    public void Review_CombinesPenalties_WhenMultipleIssuesFound()
    {
        var review = _sut.Review("Urgent vente rapide", "Courte", 3000);

        Assert.Equal(ListingStatus.Pending, review.Status);
        Assert.Equal(35, review.QualityScore);
        Assert.Contains("titre suspect", review.Reason);
        Assert.Contains("description succincte", review.Reason);
        Assert.Contains("prix élevé", review.Reason);
    }
}
