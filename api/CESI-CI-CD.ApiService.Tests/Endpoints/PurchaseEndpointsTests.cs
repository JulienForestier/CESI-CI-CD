using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class PurchaseEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PurchaseEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<Guid> CreateListingAsync(TestUser seller, decimal price = 100)
    {
        var categoryId = await GetAnyCategoryIdAsync();
        TestAuthHelper.AuthenticateAs(_client, seller);
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Catalog.Listings,
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide et détaillée", price, categoryId));
        TestAuthHelper.ClearAuth(_client);

        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        return body!.Id;
    }

    [Fact]
    public async Task Purchase_ReturnsUnauthorized_WithoutToken()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);

        var response = await _client.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_ReturnsNotFound_WhenListingUnknown()
    {
        var buyer = await RegisterUserAsync("Acheteur");

        TestAuthHelper.AuthenticateAs(_client, buyer);
        var response = await _client.PostAsync(ApiRoutes.Purchases.Purchase(Guid.NewGuid()), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_ReturnsBadRequest_WhenBuyingOwnListing()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);

        TestAuthHelper.AuthenticateAs(_client, seller);
        var response = await _client.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_MarksListingSold_AndRecordsCommission()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller, price: 200);
        var buyer = await RegisterUserAsync("Acheteur");

        TestAuthHelper.AuthenticateAs(_client, buyer);
        var response = await _client.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PurchaseResponse>(JsonOptions);
        Assert.Equal(listingId, body!.ListingId);
        Assert.Equal(200, body.Price);
        Assert.Equal(10, body.CommissionAmount); // 5% de 200

        var listingResponse = await _client.GetAsync(ApiRoutes.Catalog.ListingById(listingId));
        Assert.Equal(HttpStatusCode.NotFound, listingResponse.StatusCode); // sortie du catalogue public (Status != Published)
    }

    [Fact]
    public async Task Purchase_ReturnsConflict_WhenListingAlreadySold()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);
        var firstBuyer = await RegisterUserAsync("Premier acheteur");
        var secondBuyer = await RegisterUserAsync("Second acheteur");

        TestAuthHelper.AuthenticateAs(_client, firstBuyer);
        await _client.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);
        TestAuthHelper.ClearAuth(_client);

        TestAuthHelper.AuthenticateAs(_client, secondBuyer);
        var response = await _client.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_UnderConcurrentRequests_ExactlyOneBuyerWins()
    {
        
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller);
        var buyerA = await RegisterUserAsync("Acheteur A");
        var buyerB = await RegisterUserAsync("Acheteur B");

        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Add("X-CSRF", "1");
        clientB.DefaultRequestHeaders.Add("X-CSRF", "1");
        TestAuthHelper.AuthenticateAs(clientA, buyerA);
        TestAuthHelper.AuthenticateAs(clientB, buyerB);

        var taskA = clientA.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);
        var taskB = clientB.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);
        var results = await Task.WhenAll(taskA, taskB);

        var statusCodes = results.Select(r => r.StatusCode).OrderBy(s => s).ToList();
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statusCodes);

        // Un seul enregistrement d'achat doit exister pour cette annonce.
        var admin = await RegisterUserAsync("Admin vérif");
        TestAuthHelper.AuthenticateAs(_client, admin);
        var winner = results[0].StatusCode == HttpStatusCode.Created ? clientA : clientB;
        var purchases = await winner.GetFromJsonAsync<List<PurchaseResponse>>(ApiRoutes.Purchases.Mine, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Single(purchases!, p => p.ListingId == listingId);
    }

    [Fact]
    public async Task GetMyPurchases_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Purchases.Mine);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyPurchases_ReturnsOnlyOwnPurchases()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreateListingAsync(seller, price: 50);
        var buyer = await RegisterUserAsync("Acheteur");
        var otherUser = await RegisterUserAsync("Autre utilisateur");

        TestAuthHelper.AuthenticateAs(_client, buyer);
        await _client.PostAsync(ApiRoutes.Purchases.Purchase(listingId), null);
        var purchases = await _client.GetFromJsonAsync<List<PurchaseResponse>>(ApiRoutes.Purchases.Mine, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Contains(purchases!, p => p.ListingId == listingId);

        TestAuthHelper.AuthenticateAs(_client, otherUser);
        var otherPurchases = await _client.GetFromJsonAsync<List<PurchaseResponse>>(ApiRoutes.Purchases.Mine, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.DoesNotContain(otherPurchases!, p => p.ListingId == listingId);
    }
}
