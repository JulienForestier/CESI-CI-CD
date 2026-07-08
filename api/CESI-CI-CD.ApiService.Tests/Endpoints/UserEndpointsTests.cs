using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AuthResponse> RegisterUserAsync()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "P@ssword123", "Utilisateur Test"));

        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

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
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var profile = await _client.GetFromJsonAsync<UserProfileResponse>(ApiRoutes.Users.Me);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(user.UserId, profile!.Id);
        Assert.Equal(user.Email, profile.Email);
        Assert.Equal("Utilisateur Test", profile.DisplayName);
        Assert.False(profile.IsAdmin);
    }

    [Fact]
    public async Task PatchMe_UpdatesDisplayName()
    {
        var user = await RegisterUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.PatchAsJsonAsync(ApiRoutes.Users.Me, new UpdateProfileRequest("Nouveau pseudo"));
        var updated = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        var profile = await _client.GetFromJsonAsync<UserProfileResponse>(ApiRoutes.Users.Me);
        _client.DefaultRequestHeaders.Authorization = null;

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
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.PatchAsJsonAsync(ApiRoutes.Users.Me, new UpdateProfileRequest(displayName));
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
