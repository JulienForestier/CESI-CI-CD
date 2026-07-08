using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class NotificationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
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

    private Task<TestUser> RegisterUserAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Utilisateur Test");

    private Task<TestUser> RegisterAdminAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Admin Test", isAdmin: true);

    private async Task<Guid> GetAnyCategoryIdAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>(ApiRoutes.Catalog.Categories);
        return categories![0].Id;
    }

    private async Task SetInterestAsync(TestUser user, Guid categoryId)
    {
        TestAuthHelper.AuthenticateAs(_client, user);
        await _client.PutAsJsonAsync(ApiRoutes.Interests.Base, new UpdateInterestsRequest([categoryId]));
        TestAuthHelper.ClearAuth(_client);
    }

    private async Task<List<NotificationResponse>> GetNotificationsAsync(TestUser user)
    {
        TestAuthHelper.AuthenticateAs(_client, user);
        var notifications = await _client.GetFromJsonAsync<List<NotificationResponse>>(ApiRoutes.Notifications.Base, JsonOptions);
        TestAuthHelper.ClearAuth(_client);
        return notifications!;
    }

    [Fact]
    public async Task GetNotifications_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Notifications.Base);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PostAsync(ApiRoutes.Notifications.MarkAllRead, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotifications_ReturnsEmptyList_ForNewUser()
    {
        var user = await RegisterUserAsync();

        var notifications = await GetNotificationsAsync(user);

        Assert.Empty(notifications);
    }

    [Fact]
    public async Task PublishingAListing_NotifiesInterestedUsers_ButNotTheSeller()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var interestedBuyer = await RegisterUserAsync();
        await SetInterestAsync(interestedBuyer, categoryId);
        var uninterestedBuyer = await RegisterUserAsync();
        var seller = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, seller);
        await _client.PostAsJsonAsync(
            ApiRoutes.Catalog.Listings,
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide et détaillée", 25, categoryId));
        TestAuthHelper.ClearAuth(_client);

        var interestedNotifications = await GetNotificationsAsync(interestedBuyer);
        var uninterestedNotifications = await GetNotificationsAsync(uninterestedBuyer);
        var sellerNotifications = await GetNotificationsAsync(seller);

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
        await SetInterestAsync(interestedBuyer, categoryId);
        var seller = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, seller);
        var createResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Catalog.Listings,
            new CreateListingRequest("Vente urgente collection", "Description tout à fait normale et détaillée", 25, categoryId));
        TestAuthHelper.ClearAuth(_client);
        var listing = await createResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        Assert.Equal(ListingStatus.Pending, listing!.Status);

        var admin = await RegisterAdminAsync();
        TestAuthHelper.AuthenticateAs(_client, admin);
        await _client.PostAsync(ApiRoutes.Moderation.Approve(listing.Id), null);
        TestAuthHelper.ClearAuth(_client);

        var sellerNotifications = await GetNotificationsAsync(seller);
        var interestedNotifications = await GetNotificationsAsync(interestedBuyer);

        Assert.Single(sellerNotifications, n => n.Type == NotificationType.ListingApproved);
        Assert.Single(interestedNotifications, n => n.Type == NotificationType.NewListingMatch);
    }

    [Fact]
    public async Task RejectingAPendingListing_NotifiesTheSellerWithReason()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var seller = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, seller);
        var createResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Catalog.Listings,
            new CreateListingRequest("Vente urgente collection", "Description tout à fait normale et détaillée", 25, categoryId));
        TestAuthHelper.ClearAuth(_client);
        var listing = await createResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);

        var admin = await RegisterAdminAsync();
        TestAuthHelper.AuthenticateAs(_client, admin);
        await _client.PostAsJsonAsync(ApiRoutes.Moderation.Reject(listing!.Id), new RejectListingRequest("Titre non conforme"));
        TestAuthHelper.ClearAuth(_client);

        var sellerNotifications = await GetNotificationsAsync(seller);

        var notification = Assert.Single(sellerNotifications, n => n.Type == NotificationType.ListingRejected);
        Assert.Contains("Titre non conforme", notification.Message);
    }

    [Fact]
    public async Task MarkAllRead_MarksEveryNotificationAsRead()
    {
        var categoryId = await GetAnyCategoryIdAsync();
        var interestedBuyer = await RegisterUserAsync();
        await SetInterestAsync(interestedBuyer, categoryId);
        var seller = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, seller);
        await _client.PostAsJsonAsync(
            ApiRoutes.Catalog.Listings,
            new CreateListingRequest($"Annonce {Guid.NewGuid():N}", "Description valide et détaillée", 25, categoryId));
        TestAuthHelper.ClearAuth(_client);

        TestAuthHelper.AuthenticateAs(_client, interestedBuyer);
        var markReadResponse = await _client.PostAsync(ApiRoutes.Notifications.MarkAllRead, null);
        TestAuthHelper.ClearAuth(_client);

        var notifications = await GetNotificationsAsync(interestedBuyer);

        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);
        Assert.All(notifications, n => Assert.True(n.IsRead));
    }
}
