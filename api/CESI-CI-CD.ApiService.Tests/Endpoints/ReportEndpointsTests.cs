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

    private Task<TestUser> RegisterAdminAsync() =>
        TestAuthHelper.CreateUserAsync(_factory, displayName: "Admin Test", isAdmin: true);

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
    public async Task Report_ReturnsUnauthorized_WithoutToken()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(listingId), new CreateReportRequest("Contenu suspect", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Report_ReturnsBadRequest_WhenReasonMissing()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);
        var reporter = await RegisterUserAsync("Signaleur");

        TestAuthHelper.AuthenticateAs(_client, reporter);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(listingId), new CreateReportRequest("   ", null));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_ReturnsNotFound_WhenListingUnknown()
    {
        var reporter = await RegisterUserAsync("Signaleur");

        TestAuthHelper.AuthenticateAs(_client, reporter);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(Guid.NewGuid()), new CreateReportRequest("Contenu suspect", null));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Report_ReturnsBadRequest_WhenReportingOwnListing()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);

        TestAuthHelper.AuthenticateAs(_client, seller);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(listingId), new CreateReportRequest("Contenu suspect", null));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    [Fact]
    public async Task Report_ReturnsConflict_WhenAlreadyReportedBySameUser()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);
        var reporter = await RegisterUserAsync("Signaleur");

        TestAuthHelper.AuthenticateAs(_client, reporter);
        await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(listingId), new CreateReportRequest("Contenu suspect", null));
        var second = await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(listingId), new CreateReportRequest("Autre motif", null));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetReports_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Reports.AdminList);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReports_ReturnsForbidden_ForNonAdmin()
    {
        var user = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, user);
        var response = await _client.GetAsync(ApiRoutes.Reports.AdminList);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReports_ReturnsAllReports_ForAdmin()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);
        var reporter = await RegisterUserAsync("Signaleur");

        TestAuthHelper.AuthenticateAs(_client, reporter);
        await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(listingId), new CreateReportRequest("Contenu suspect", "Détail du signalement"));
        TestAuthHelper.ClearAuth(_client);

        var admin = await RegisterAdminAsync();
        TestAuthHelper.AuthenticateAs(_client, admin);
        var reports = await _client.GetFromJsonAsync<List<ReportResponse>>(ApiRoutes.Reports.AdminList, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Contains(reports!, r => r.ListingId == listingId && r.Reason == "Contenu suspect");
    }

    [Fact]
    public async Task GetReports_FiltersBySearch_CaseInsensitive()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);
        var reporter = await RegisterUserAsync("Signaleur");

        TestAuthHelper.AuthenticateAs(_client, reporter);
        await _client.PostAsJsonAsync(ApiRoutes.Reports.Report(listingId), new CreateReportRequest("Paiement hors plateforme", null));
        TestAuthHelper.ClearAuth(_client);

        var admin = await RegisterAdminAsync();
        TestAuthHelper.AuthenticateAs(_client, admin);
        var matched = await _client.GetFromJsonAsync<List<ReportResponse>>($"{ApiRoutes.Reports.AdminList}?search=PAIEMENT", JsonOptions);
        var unmatched = await _client.GetFromJsonAsync<List<ReportResponse>>($"{ApiRoutes.Reports.AdminList}?search=inexistant", JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Contains(matched!, r => r.ListingId == listingId);
        Assert.DoesNotContain(unmatched!, r => r.ListingId == listingId);
    }
}
