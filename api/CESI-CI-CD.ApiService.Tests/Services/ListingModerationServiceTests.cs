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
}
