using System.Net;
using System.Net.Http.Json;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class InterestEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InterestEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<TestUser> RegisterUserAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Utilisateur Test");

    private async Task<List<Guid>> GetAllCategoryIdsAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>(ApiRoutes.Catalog.Categories);
        return categories!.Select(c => c.Id).ToList();
    }

    [Fact]
    public async Task GetInterests_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Interests.Base);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutInterests_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PutAsJsonAsync(ApiRoutes.Interests.Base, new UpdateInterestsRequest([]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInterests_ReturnsEmptyList_ForNewUser()
    {
        var user = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var interests = await _client.GetFromJsonAsync<List<Guid>>(ApiRoutes.Interests.Base);
        TestAuthHelper.ClearAuth(_client);

        Assert.Empty(interests!);
    }

    [Fact]
    public async Task PutInterests_ReturnsBadRequest_WhenCategoryUnknown()
    {
        var user = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var response = await _client.PutAsJsonAsync(ApiRoutes.Interests.Base, new UpdateInterestsRequest([Guid.NewGuid()]));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutInterests_SavesSelection_AndReplacesPreviousOne()
    {
        var categoryIds = await GetAllCategoryIdsAsync();
        var user = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var first = await _client.PutAsJsonAsync(ApiRoutes.Interests.Base, new UpdateInterestsRequest([categoryIds[0], categoryIds[1]]));
        var afterFirst = await _client.GetFromJsonAsync<List<Guid>>(ApiRoutes.Interests.Base);

        var second = await _client.PutAsJsonAsync(ApiRoutes.Interests.Base, new UpdateInterestsRequest([categoryIds[2]]));
        var afterSecond = await _client.GetFromJsonAsync<List<Guid>>(ApiRoutes.Interests.Base);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(2, afterFirst!.Count);
        Assert.Contains(categoryIds[0], afterFirst);
        Assert.Contains(categoryIds[1], afterFirst);
        Assert.Single(afterSecond!, categoryIds[2]);
    }
}
