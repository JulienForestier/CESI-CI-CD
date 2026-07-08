using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class ChatEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChatEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<TestUser> RegisterUserAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Utilisateur Test");

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

    private async Task<Guid> StartConversationAsync(TestUser buyer, Guid listingId)
    {
        TestAuthHelper.AuthenticateAs(_client, buyer);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Conversations, new StartConversationRequest(listingId));
        TestAuthHelper.ClearAuth(_client);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task StartConversation_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Conversations, new StartConversationRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConversations_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Chat.Conversations);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Chat.Messages(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Chat.Messages(Guid.NewGuid()),
            new SendMessageRequest("Bonjour"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_ReturnsNotFound_WhenListingUnknown()
    {
        var buyer = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, buyer);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Conversations, new StartConversationRequest(Guid.NewGuid()));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_ReturnsBadRequest_WhenStartingWithOwnListing()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);

        TestAuthHelper.AuthenticateAs(_client, seller);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Conversations, new StartConversationRequest(listingId));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_IsIdempotent_ReturnsSameConversationId()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterUserAsync();

        var firstId = await StartConversationAsync(buyer, listingId);
        var secondId = await StartConversationAsync(buyer, listingId);

        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task GetConversations_ReturnsConversation_ForBothBuyerAndSeller()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer, listingId);

        TestAuthHelper.AuthenticateAs(_client, buyer);
        var buyerConversations = await _client.GetFromJsonAsync<List<ConversationResponse>>(ApiRoutes.Chat.Conversations, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        TestAuthHelper.AuthenticateAs(_client, seller);
        var sellerConversations = await _client.GetFromJsonAsync<List<ConversationResponse>>(ApiRoutes.Chat.Conversations, JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Contains(buyerConversations!, c => c.Id == conversationId && c.CounterpartId == seller.UserId);
        Assert.Contains(sellerConversations!, c => c.Id == conversationId && c.CounterpartId == buyer.UserId);
    }

    [Theory]
    [InlineData("Contactez-moi à jean.dupont@gmail.com")]
    [InlineData("Appelez au 06 12 34 56 78")]
    public async Task SendMessage_RejectsMessage_ContainingContactInfo(string body)
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer, listingId);

        TestAuthHelper.AuthenticateAs(_client, buyer);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Messages(conversationId), new SendMessageRequest(body));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_RejectsEmptyMessage()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer, listingId);

        TestAuthHelper.AuthenticateAs(_client, buyer);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Messages(conversationId), new SendMessageRequest("   "));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_AcceptsNormalMessage_AndAppearsInGetMessages()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer, listingId);

        TestAuthHelper.AuthenticateAs(_client, buyer);
        var sendResponse = await _client.PostAsJsonAsync(
            ApiRoutes.Chat.Messages(conversationId),
            new SendMessageRequest("Bonjour, l'objet est-il toujours disponible ?"));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Created, sendResponse.StatusCode);

        TestAuthHelper.AuthenticateAs(_client, seller);
        var messages = await _client.GetFromJsonAsync<List<MessageResponse>>(ApiRoutes.Chat.Messages(conversationId), JsonOptions);
        TestAuthHelper.ClearAuth(_client);

        Assert.Single(messages!, m => m.Body == "Bonjour, l'objet est-il toujours disponible ?" && m.SenderId == buyer.UserId);
    }

    [Fact]
    public async Task GetMessages_ReturnsForbidden_ForNonParticipant()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer, listingId);
        var stranger = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, stranger);
        var response = await _client.GetAsync(ApiRoutes.Chat.Messages(conversationId));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsForbidden_ForNonParticipant()
    {
        var seller = await RegisterUserAsync();
        var listingId = await CreatePublishedListingAsync(seller);
        var buyer = await RegisterUserAsync();
        var conversationId = await StartConversationAsync(buyer, listingId);
        var stranger = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, stranger);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Messages(conversationId), new SendMessageRequest("Salut"));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsNotFound_WhenConversationUnknown()
    {
        var user = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, user);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Chat.Messages(Guid.NewGuid()), new SendMessageRequest("Salut"));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ReturnsNotFound_WhenConversationUnknown()
    {
        var user = await RegisterUserAsync();

        TestAuthHelper.AuthenticateAs(_client, user);
        var response = await _client.GetAsync(ApiRoutes.Chat.Messages(Guid.NewGuid()));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
