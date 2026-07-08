using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class ModerationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "P@ssword123";

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

    private async Task<AuthResponse> RegisterUserAsync()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, Password, "Utilisateur Test"));

        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private async Task<string> RegisterAdminAsync()
    {
        var user = await RegisterUserAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CollectorShopDbContext>();
        var entity = await db.Users.FirstAsync(u => u.Id == user.UserId);
        entity.IsAdmin = true;
        await db.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, Password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return loginBody!.Token;
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
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide et détaillée", 25, categoryId));
        _client.DefaultRequestHeaders.Authorization = null;

        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        return body!.Id;
    }

    private async Task<Guid> CreatePendingListingAsync(string sellerToken)
    {
        var categoryId = await GetAnyCategoryIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
        var response = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("Vente urgente collection", "Description tout à fait normale et détaillée", 25, categoryId));
        _client.DefaultRequestHeaders.Authorization = null;

        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Pending, body!.Status);
        return body.Id;
    }

    [Fact]
    public async Task GetPendingListings_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/admin/listings/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingListings_ReturnsForbidden_ForNonAdmin()
    {
        var user = await RegisterUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.GetAsync("/api/admin/listings/pending");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingListings_ReturnsOnlyPendingListings_ForAdmin()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller.Token);
        var publishedId = await CreatePublishedListingAsync(seller.Token);
        var adminToken = await RegisterAdminAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var pending = await _client.GetFromJsonAsync<List<ListingResponse>>("/api/admin/listings/pending", JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Contains(pending!, l => l.Id == pendingId);
        Assert.DoesNotContain(pending!, l => l.Id == publishedId);
    }

    [Fact]
    public async Task ApproveListing_PublishesListing()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller.Token);
        var adminToken = await RegisterAdminAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.PostAsync($"/api/admin/listings/{pendingId}/approve", null);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Published, body!.Status);

        var publicListing = await _client.GetFromJsonAsync<ListingResponse>($"/api/listings/{pendingId}", JsonOptions);
        Assert.NotNull(publicListing);
    }

    [Fact]
    public async Task ApproveListing_ReturnsNotFound_WhenListingUnknown()
    {
        var adminToken = await RegisterAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PostAsync($"/api/admin/listings/{Guid.NewGuid()}/approve", null);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApproveListing_ReturnsBadRequest_WhenListingNotPending()
    {
        var seller = await RegisterUserAsync();
        var publishedId = await CreatePublishedListingAsync(seller.Token);
        var adminToken = await RegisterAdminAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.PostAsync($"/api/admin/listings/{publishedId}/approve", null);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectListing_ReturnsForbidden_ForNonAdmin()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller.Token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        var response = await _client.PostAsJsonAsync($"/api/admin/listings/{pendingId}/reject", new RejectListingRequest("Titre non conforme"));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RejectListing_SetsRejectedStatus_WithReason()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller.Token);
        var adminToken = await RegisterAdminAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.PostAsJsonAsync($"/api/admin/listings/{pendingId}/reject", new RejectListingRequest("Titre non conforme à la charte"));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Rejected, body!.Status);
        Assert.Equal("Titre non conforme à la charte", body.ModerationReason);
    }

    [Fact]
    public async Task RejectListing_ReturnsBadRequest_WhenReasonMissing()
    {
        var seller = await RegisterUserAsync();
        var pendingId = await CreatePendingListingAsync(seller.Token);
        var adminToken = await RegisterAdminAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.PostAsJsonAsync($"/api/admin/listings/{pendingId}/reject", new RejectListingRequest("   "));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectListing_ReturnsBadRequest_WhenListingNotPending()
    {
        var seller = await RegisterUserAsync();
        var publishedId = await CreatePublishedListingAsync(seller.Token);
        var adminToken = await RegisterAdminAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.PostAsJsonAsync($"/api/admin/listings/{publishedId}/reject", new RejectListingRequest("Motif"));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
