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

public class NotificationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "P@ssword123";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task SetInterestAsync(string token, Guid categoryId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PutAsJsonAsync("/api/interests", new UpdateInterestsRequest([categoryId]));
        _client.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<List<NotificationResponse>> GetNotificationsAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var notifications = await _client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications", JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;
        return notifications!;
    }

    [Fact]
    public async Task GetNotifications_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PostAsync("/api/notifications/mark-all-read", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotifications_ReturnsEmptyList_ForNewUser()
    {
        var user = await RegisterUserAsync();

        var notifications = await GetNotificationsAsync(user.Token);

        Assert.Empty(notifications);
    }

    [Fact]
    public async Task PublishingAListing_NotifiesInterestedUsers_ButNotTheSeller()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var interestedBuyer = await RegisterUserAsync();
        await SetInterestAsync(interestedBuyer.Token, categoryId);
        var uninterestedBuyer = await RegisterUserAsync();
        var seller = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide et détaillée", 25, categoryId));
        _client.DefaultRequestHeaders.Authorization = null;

        var interestedNotifications = await GetNotificationsAsync(interestedBuyer.Token);
        var uninterestedNotifications = await GetNotificationsAsync(uninterestedBuyer.Token);
        var sellerNotifications = await GetNotificationsAsync(seller.Token);

        Assert.Single(interestedNotifications, n => n.Type == NotificationType.NewListingMatch);
        Assert.False(interestedNotifications[0].IsRead);
        Assert.Empty(uninterestedNotifications);
        Assert.Empty(sellerNotifications);
    }

    [Fact]
    public async Task ApprovingAPendingListing_NotifiesTheSeller_AndInterestedUsers()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var interestedBuyer = await RegisterUserAsync();
        await SetInterestAsync(interestedBuyer.Token, categoryId);
        var seller = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("Vente urgente collection", "Description tout à fait normale et détaillée", 25, categoryId));
        _client.DefaultRequestHeaders.Authorization = null;
        var listing = await createResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Pending, listing!.Status);

        var adminToken = await RegisterAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsync($"/api/admin/listings/{listing.Id}/approve", null);
        _client.DefaultRequestHeaders.Authorization = null;

        var sellerNotifications = await GetNotificationsAsync(seller.Token);
        var interestedNotifications = await GetNotificationsAsync(interestedBuyer.Token);

        Assert.Single(sellerNotifications, n => n.Type == NotificationType.ListingApproved);
        Assert.Single(interestedNotifications, n => n.Type == NotificationType.NewListingMatch);
    }

    [Fact]
    public async Task RejectingAPendingListing_NotifiesTheSellerWithReason()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var seller = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest("Vente urgente collection", "Description tout à fait normale et détaillée", 25, categoryId));
        _client.DefaultRequestHeaders.Authorization = null;
        var listing = await createResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);

        var adminToken = await RegisterAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsJsonAsync($"/api/admin/listings/{listing!.Id}/reject", new RejectListingRequest("Titre non conforme"));
        _client.DefaultRequestHeaders.Authorization = null;

        var sellerNotifications = await GetNotificationsAsync(seller.Token);

        var notification = Assert.Single(sellerNotifications, n => n.Type == NotificationType.ListingRejected);
        Assert.Contains("Titre non conforme", notification.Message);
    }

    [Fact]
    public async Task MarkAllRead_MarksEveryNotificationAsRead()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var interestedBuyer = await RegisterUserAsync();
        await SetInterestAsync(interestedBuyer.Token, categoryId);
        var seller = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        await _client.PostAsJsonAsync(
            "/api/listings",
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide et détaillée", 25, categoryId));
        _client.DefaultRequestHeaders.Authorization = null;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", interestedBuyer.Token);
        var markReadResponse = await _client.PostAsync("/api/notifications/mark-all-read", null);
        _client.DefaultRequestHeaders.Authorization = null;

        var notifications = await GetNotificationsAsync(interestedBuyer.Token);

        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);
        Assert.All(notifications, n => Assert.True(n.IsRead));
    }
}
