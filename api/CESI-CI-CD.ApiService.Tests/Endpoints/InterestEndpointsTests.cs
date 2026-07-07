using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CESI_CI_CD.ApiService.Contracts;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class InterestEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InterestEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterUserAsync()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "P@ssword123", "Utilisateur Test"));

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private async Task<List<Guid>> GetAllCategoryIdsAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        return categories!.Select(c => c.Id).ToList();
    }

    [Fact]
    public async Task GetInterests_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/interests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutInterests_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PutAsJsonAsync("/api/interests", new UpdateInterestsRequest([]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInterests_ReturnsEmptyList_ForNewUser()
    {
        var token = await RegisterUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var interests = await _client.GetFromJsonAsync<List<Guid>>("/api/interests");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Empty(interests!);
    }

    [Fact]
    public async Task PutInterests_ReturnsBadRequest_WhenCategoryUnknown()
    {
        var token = await RegisterUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync("/api/interests", new UpdateInterestsRequest([Guid.NewGuid()]));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutInterests_SavesSelection_AndReplacesPreviousOne()
    {
        var categoryIds = await GetAllCategoryIdsAsync();
        var token = await RegisterUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var first = await _client.PutAsJsonAsync("/api/interests", new UpdateInterestsRequest([categoryIds[0], categoryIds[1]]));
        var afterFirst = await _client.GetFromJsonAsync<List<Guid>>("/api/interests");

        var second = await _client.PutAsJsonAsync("/api/interests", new UpdateInterestsRequest([categoryIds[2]]));
        var afterSecond = await _client.GetFromJsonAsync<List<Guid>>("/api/interests");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(2, afterFirst!.Count);
        Assert.Contains(categoryIds[0], afterFirst);
        Assert.Contains(categoryIds[1], afterFirst);
        Assert.Single(afterSecond!, categoryIds[2]);
    }
}
