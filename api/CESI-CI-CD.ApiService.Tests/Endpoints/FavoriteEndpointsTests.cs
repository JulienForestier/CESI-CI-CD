using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class FavoriteEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FavoriteEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<TestUser> RegisterSellerAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Vendeur Test");

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
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide", 25, categoryId));
        TestAuthHelper.ClearAuth(_client);

        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        return body!.Id;
    }

    [Fact]
    public async Task GetFavorites_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Favorites.List);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFavoriteIds_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Favorites.FavoriteIds);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutFavorite_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PutAsync(ApiRoutes.Favorites.Toggle(Guid.NewGuid()), null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutFavorite_ReturnsNotFound_WhenListingUnknown()
    {
        var user = await RegisterSellerAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var response = await _client.PutAsync(ApiRoutes.Favorites.Toggle(Guid.NewGuid()), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutFavorite_AddsListingToFavorites_AndIsIdempotent()
    {
        var seller = await RegisterSellerAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterSellerAsync();
        TestAuthHelper.AuthenticateAs(_client, buyer);

        var first = await _client.PutAsync(ApiRoutes.Favorites.Toggle(listingId), null);
        var second = await _client.PutAsync(ApiRoutes.Favorites.Toggle(listingId), null);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var ids = await _client.GetFromJsonAsync<List<Guid>>(ApiRoutes.Favorites.FavoriteIds);
        TestAuthHelper.ClearAuth(_client);

        Assert.Single(ids!, id => id == listingId);
    }

    [Fact]
    public async Task DeleteFavorite_RemovesFavorite_AndIsIdempotentWhenNotFavorited()
    {
        var seller = await RegisterSellerAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterSellerAsync();
        TestAuthHelper.AuthenticateAs(_client, buyer);

        await _client.PutAsync(ApiRoutes.Favorites.Toggle(listingId), null);
        var firstDelete = await _client.DeleteAsync(ApiRoutes.Favorites.Toggle(listingId));
        var secondDelete = await _client.DeleteAsync(ApiRoutes.Favorites.Toggle(listingId));

        var ids = await _client.GetFromJsonAsync<List<Guid>>(ApiRoutes.Favorites.FavoriteIds);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondDelete.StatusCode);
        Assert.DoesNotContain(listingId, ids!);
    }

    [Fact]
    public async Task GetFavorites_ReturnsOnlyCurrentUsersFavorites()
    {
        var seller = await RegisterSellerAsync();
        var listingId = await CreatePublishedListingAsync(seller);

        var buyer = await RegisterSellerAsync();
        TestAuthHelper.AuthenticateAs(_client, buyer);
        await _client.PutAsync(ApiRoutes.Favorites.Toggle(listingId), null);
        var buyerFavorites = await _client.GetFromJsonAsync<List<ListingResponse>>(ApiRoutes.Favorites.List, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        var other = await RegisterSellerAsync();
        TestAuthHelper.AuthenticateAs(_client, other);
        var otherFavorites = await _client.GetFromJsonAsync<List<ListingResponse>>(ApiRoutes.Favorites.List, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Contains(buyerFavorites!, l => l.Id == listingId);
        Assert.DoesNotContain(otherFavorites!, l => l.Id == listingId);
    }
}
