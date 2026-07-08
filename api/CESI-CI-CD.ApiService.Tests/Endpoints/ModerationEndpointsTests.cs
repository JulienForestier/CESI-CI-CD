using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class ModerationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ModerationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<TestUser> RegisterUserAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Utilisateur Test");

    private Task<TestUser> RegisterAdminAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Admin Test", isAdmin: true);

    private async Task<Guid> GetAnyCategoryIdAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>(ApiRoutes.Catalog.Categories);
        return categories![0].Id;
    }

    private async Task<Guid> CreatePublishedListingAsync(TestUser seller)
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

    private async Task<Guid> CreatePendingListingAsync(TestUser seller)
    {
        var categoryId = await GetAnyCategoryIdAsync();
        TestAuthHelper.AuthenticateAs(_client, seller);
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Catalog.Listings,
            new CreateListingRequest("Vente urgente collection", "Description tout à fait normale et détaillée", 25, categoryId));
        TestAuthHelper.ClearAuth(_client);

        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Pending, body!.Status);
        return body.Id;
    }

    [Fact]
    public async Task GetPendingListings_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Moderation.PendingListings);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingListings_ReturnsForbidden_ForNonAdmin()
    {
        var user = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var response = await _client.GetAsync(ApiRoutes.Moderation.PendingListings);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingListings_ReturnsOnlyPendingListings_ForAdmin()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller);
        var publishedId = await CreatePublishedListingAsync(seller);
        var admin = await RegisterAdminAsync();

        TestAuthHelper.AuthenticateAs(_client, admin);
        var pending = await _client.GetFromJsonAsync<List<ListingResponse>>(ApiRoutes.Moderation.PendingListings, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Contains(pending!, l => l.Id == pendingId);
        Assert.DoesNotContain(pending!, l => l.Id == publishedId);
    }

    [Fact]
    public async Task ApproveListing_PublishesListing()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller);
        var admin = await RegisterAdminAsync();

        TestAuthHelper.AuthenticateAs(_client, admin);
        var response = await _client.PostAsync(ApiRoutes.Moderation.Approve(pendingId), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Published, body!.Status);

        var publicListing = await _client.GetFromJsonAsync<ListingResponse>(ApiRoutes.Catalog.ListingById(pendingId), JsonOptions);
        Assert.NotNull(publicListing);
    }

    [Fact]
    public async Task ApproveListing_ReturnsNotFound_WhenListingUnknown()
    {
        var admin = await RegisterAdminAsync();
        TestAuthHelper.AuthenticateAs(_client, admin);

        var response = await _client.PostAsync(ApiRoutes.Moderation.Approve(Guid.NewGuid()), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApproveListing_ReturnsBadRequest_WhenListingNotPending()
    {
        var seller = await RegisterUserAsync();
        var publishedId = await CreatePublishedListingAsync(seller);
        var admin = await RegisterAdminAsync();

        TestAuthHelper.AuthenticateAs(_client, admin);
        var response = await _client.PostAsync(ApiRoutes.Moderation.Approve(publishedId), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectListing_ReturnsForbidden_ForNonAdmin()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller);

        TestAuthHelper.AuthenticateAs(_client, seller);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Moderation.Reject(pendingId), new RejectListingRequest("Titre non conforme"));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RejectListing_SetsRejectedStatus_WithReason()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller);
        var admin = await RegisterAdminAsync();

        TestAuthHelper.AuthenticateAs(_client, admin);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Moderation.Reject(pendingId), new RejectListingRequest("Titre non conforme à la charte"));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Rejected, body!.Status);
        Assert.Equal("Titre non conforme à la charte", body.ModerationReason);
    }

    [Fact]
    public async Task RejectListing_ReturnsBadRequest_WhenReasonMissing()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller);
        var admin = await RegisterAdminAsync();

        TestAuthHelper.AuthenticateAs(_client, admin);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Moderation.Reject(pendingId), new RejectListingRequest("   "));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectListing_ReturnsBadRequest_WhenListingNotPending()
    {
        var seller = await RegisterUserAsync();
        var publishedId = await CreatePublishedListingAsync(seller);
        var admin = await RegisterAdminAsync();

        TestAuthHelper.AuthenticateAs(_client, admin);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Moderation.Reject(publishedId), new RejectListingRequest("Motif"));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
