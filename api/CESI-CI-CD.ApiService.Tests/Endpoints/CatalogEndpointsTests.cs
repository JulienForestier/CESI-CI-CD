using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data.Entities;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class CatalogEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public CatalogEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId)> RegisterSellerAsync()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "P@ssword123", "Vendeur Test"));

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (body!.Token, body.UserId);
    }

    private async Task<Guid> GetAnyCategoryIdAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        return categories![0].Id;
    }

    [Fact]
    public async Task GetCategories_ReturnsSeededCategories()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");

        Assert.NotNull(categories);
        Assert.True(categories!.Count >= 3);
    }

    [Fact]
    public async Task GetListings_ReturnsOnlyPublishedListings()
    {
        var response = await _client.GetAsync("/api/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listings = await response.Content.ReadFromJsonAsync<List<ListingResponse>>(JsonOptions);
        Assert.NotNull(listings);
        Assert.All(listings!, l => Assert.Equal(ListingStatus.Published, l.Status));
    }

    [Fact]
    public async Task GetListingById_ReturnsNotFound_WhenUnknown()
    {
        var response = await _client.GetAsync($"/api/listings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostListing_ReturnsUnauthorized_WithoutToken()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var response = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("Titre valide", "Description valide", 42, categoryId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostListing_CreatesPublishedListing_WhenValid()
    {
        var (token, sellerId) = await RegisterSellerAsync();
        var categoryId = await GetAnyCategoryIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("Figurine rare", "En excellent état", 99.90m, categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Published, listing!.Status);
        Assert.Equal(sellerId, listing.SellerId);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task PostListing_CreatesRejectedListing_WhenModerationFails()
    {
        var (token, _) = await RegisterSellerAsync();
        var categoryId = await GetAnyCategoryIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("ab", "", -5, categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Rejected, listing!.Status);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task PostListing_ReturnsBadRequest_WhenCategoryUnknown()
    {
        var (token, _) = await RegisterSellerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("Titre valide", "Description valide", 10, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetListingById_ReturnsListing_AfterPublish()
    {
        var (token, _) = await RegisterSellerAsync();
        var categoryId = await GetAnyCategoryIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("Sneakers collector", "Jamais portées, boîte incluse", 250, categoryId));
        var created = await createResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/listings/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listing = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(created.Id, listing!.Id);
    }
}
