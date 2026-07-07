using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class ChatEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public ChatEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AuthResponse> RegisterUserAsync()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "P@ssword123", "Utilisateur Test"));

        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
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

    private async Task<Guid> StartConversationAsync(string buyerToken, Guid listingId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        var response = await _client.PostAsJsonAsync("/api/conversations", new StartConversationRequest(listingId));
        _client.DefaultRequestHeaders.Authorization = null;

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task StartConversation_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PostAsJsonAsync("/api/conversations", new StartConversationRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConversations_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/conversations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync($"/api/conversations/{Guid.NewGuid()}/messages");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/conversations/{Guid.NewGuid()}/messages",
            new SendMessageRequest("Bonjour"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_ReturnsNotFound_WhenListingUnknown()
    {
        var buyer = await RegisterUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyer.Token);

        var response = await _client.PostAsJsonAsync("/api/conversations", new StartConversationRequest(Guid.NewGuid()));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_ReturnsBadRequest_WhenStartingWithOwnListing()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        var response = await _client.PostAsJsonAsync("/api/conversations", new StartConversationRequest(listingId));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_IsIdempotent_ReturnsSameConversationId()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);
        var buyer = await RegisterUserAsync();

        var firstId = await StartConversationAsync(buyer.Token, listingId);
        var secondId = await StartConversationAsync(buyer.Token, listingId);

        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task GetConversations_ReturnsConversation_ForBothBuyerAndSeller()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer.Token, listingId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyer.Token);
        var buyerConversations = await _client.GetFromJsonAsync<List<ConversationResponse>>("/api/conversations", JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        var sellerConversations = await _client.GetFromJsonAsync<List<ConversationResponse>>("/api/conversations", JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Contains(buyerConversations!, c => c.Id == conversationId && c.CounterpartId == seller.UserId);
        Assert.Contains(sellerConversations!, c => c.Id == conversationId && c.CounterpartId == buyer.UserId);
    }

    [Theory]
    [InlineData("Contactez-moi à jean.dupont@gmail.com")]
    [InlineData("Appelez au 06 12 34 56 78")]
    public async Task SendMessage_RejectsMessage_ContainingContactInfo(string body)
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer.Token, listingId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyer.Token);
        var response = await _client.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new SendMessageRequest(body));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_RejectsEmptyMessage()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer.Token, listingId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyer.Token);
        var response = await _client.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new SendMessageRequest("   "));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_AcceptsNormalMessage_AndAppearsInGetMessages()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer.Token, listingId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyer.Token);
        var sendResponse = await _client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new SendMessageRequest("Bonjour, l'objet est-il toujours disponible ?"));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Created, sendResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seller.Token);
        var messages = await _client.GetFromJsonAsync<List<MessageResponse>>($"/api/conversations/{conversationId}/messages", JsonOptions);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Single(messages!, m => m.Body == "Bonjour, l'objet est-il toujours disponible ?" && m.SenderId == buyer.UserId);
    }

    [Fact]
    public async Task GetMessages_ReturnsForbidden_ForNonParticipant()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer.Token, listingId);
        var stranger = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", stranger.Token);
        var response = await _client.GetAsync($"/api/conversations/{conversationId}/messages");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsForbidden_ForNonParticipant()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller.Token);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer.Token, listingId);
        var stranger = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", stranger.Token);
        var response = await _client.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new SendMessageRequest("Salut"));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsNotFound_WhenConversationUnknown()
    {
        var user = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var response = await _client.PostAsJsonAsync($"/api/conversations/{Guid.NewGuid()}/messages", new SendMessageRequest("Salut"));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ReturnsNotFound_WhenConversationUnknown()
    {
        var user = await RegisterUserAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var response = await _client.GetAsync($"/api/conversations/{Guid.NewGuid()}/messages");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
