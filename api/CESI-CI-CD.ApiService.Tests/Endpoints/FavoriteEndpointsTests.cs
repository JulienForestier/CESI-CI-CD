using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class FavoriteEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public FavoriteEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterSellerAsync()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "P@ssword123", "Vendeur Test"));

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private async Task<Guid> GetAnyCategoryIdAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        return categories![0].Id;
    }

    private async Task<Guid> CreatePublishedListingAsync(string sellerToken)
    {
        var categoryId = await GetAnyCategoryIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
        var response = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide", 25, categoryId));
        _client.DefaultRequestHeaders.Authorization = null;

        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        return body!.Id;
    }

    [Fact]
    public async Task GetFavorites_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/favorites");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFavoriteIds_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/favorites/ids");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutFavorite_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PutAsync($"/api/listings/{Guid.NewGuid()}/favorite", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutFavorite_ReturnsNotFound_WhenListingUnknown()
    {
        var token = await RegisterSellerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsync($"/api/listings/{Guid.NewGuid()}/favorite", null);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutFavorite_AddsListingToFavorites_AndIsIdempotent()
    {
        var sellerToken = await RegisterSellerAsync();
        var listingId = await CreatePublishedListingAsync(sellerToken);
        var buyerToken = await RegisterSellerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);

        var first = await _client.PutAsync($"/api/listings/{listingId}/favorite", null);
        var second = await _client.PutAsync($"/api/listings/{listingId}/favorite", null);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var ids = await _client.GetFromJsonAsync<List<Guid>>("/api/favorites/ids");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Single(ids!, id => id == listingId);
    }

    [Fact]
    public async Task DeleteFavorite_RemovesFavorite_AndIsIdempotentWhenNotFavorited()
    {
        var sellerToken = await RegisterSellerAsync();
        var listingId = await CreatePublishedListingAsync(sellerToken);
        var buyerToken = await RegisterSellerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);

        await _client.PutAsync($"/api/listings/{listingId}/favorite", null);
        var firstDelete = await _client.DeleteAsync($"/api/listings/{listingId}/favorite");
        var secondDelete = await _client.DeleteAsync($"/api/listings/{listingId}/favorite");

        var ids = await _client.GetFromJsonAsync<List<Guid>>("/api/favorites/ids");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondDelete.StatusCode);
        Assert.DoesNotContain(listingId, ids!);
    }

    [Fact]
    public async Task GetFavorites_ReturnsOnlyCurrentUsersFavorites()
    {
        var sellerToken = await RegisterSellerAsync();
        var listingId = await CreatePublishedListingAsync(sellerToken);

        var buyerToken = await RegisterSellerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        await _client.PutAsync($"/api/listings/{listingId}/favorite", null);
        var buyerFavorites = await _client.GetFromJsonAsync<List<ListingResponse>>("/api/favorites", JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;

        var otherToken = await RegisterSellerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var otherFavorites = await _client.GetFromJsonAsync<List<ListingResponse>>("/api/favorites", JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Contains(buyerFavorites!, l => l.Id == listingId);
        Assert.DoesNotContain(otherFavorites!, l => l.Id == listingId);
    }
}
