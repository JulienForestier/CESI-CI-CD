using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class ReportEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<TestUser> RegisterUserAsync(string displayName = "Utilisateur Test") =>
        TestAuthHelper.CreateUserAsync(_factory, displayName: displayName);

    private async Task<Guid> GetAnyCategoryIdAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>(ApiRoutes.Catalog.Categories);
        return categories![0].Id;
    }

    private async Task<Guid> CreateListingAsync(TestUser seller)
    {
        var categoryId = await GetAnyCategoryIdAsync();
        TestAuthHelper.AuthenticateAs(_client, seller);
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Catalog.Listings,
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide et détaillée", 25, categoryId));
        TestAuthHelper.ClearAuth(_client);

        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        return body!.Id;
    }

    [Fact]
    public async Task Report_CreatesReport_WhenValid()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);
        var reporter = await RegisterUserAsync("Signaleur");

        TestAuthHelper.AuthenticateAs(_client, reporter);
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Reports.Report(listingId),
            new CreateReportRequest("Contenu suspect", "Le vendeur demande un paiement hors plateforme."));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportResponse>(JsonOptions);
        Assert.Equal(listingId, body!.ListingId);
        Assert.Equal("Contenu suspect", body.Reason);
        Assert.Equal(reporter.UserId, body.ReporterId);
    }
}
