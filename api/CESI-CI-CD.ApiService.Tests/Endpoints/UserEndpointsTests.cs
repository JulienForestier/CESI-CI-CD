using System.Net;
using System.Net.Http.Json;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<TestUser> RegisterUserAsync() => TestAuthHelper.CreateUserAsync(_factory, displayName: "Utilisateur Test");

    [Fact]
    public async Task GetMe_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync(ApiRoutes.Users.Me);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchMe_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.PatchAsJsonAsync(ApiRoutes.Users.Me, new UpdateProfileRequest("Nouveau pseudo"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ReturnsTheCurrentUsersProfile()
    {
        var user = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var profile = await _client.GetFromJsonAsync<UserProfileResponse>(ApiRoutes.Users.Me);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(user.UserId, profile!.Id);
        Assert.Equal(user.Email, profile.Email);
        Assert.Equal("Utilisateur Test", profile.DisplayName);
        Assert.False(profile.IsAdmin);
    }

    [Fact]
    public async Task PatchMe_UpdatesDisplayName()
    {
        var user = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var response = await _client.PatchAsJsonAsync(ApiRoutes.Users.Me, new UpdateProfileRequest("Nouveau pseudo"));
        var updated = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        var profile = await _client.GetFromJsonAsync<UserProfileResponse>(ApiRoutes.Users.Me);
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Nouveau pseudo", updated!.DisplayName);
        Assert.Equal("Nouveau pseudo", profile!.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PatchMe_ReturnsBadRequest_WhenDisplayNameBlank(string displayName)
    {
        var user = await RegisterUserAsync();
        TestAuthHelper.AuthenticateAs(_client, user);

        var response = await _client.PatchAsJsonAsync(ApiRoutes.Users.Me, new UpdateProfileRequest(displayName));
        TestAuthHelper.ClearAuth(_client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
