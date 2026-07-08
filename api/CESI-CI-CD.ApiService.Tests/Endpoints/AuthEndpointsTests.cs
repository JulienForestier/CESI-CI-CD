using System.Net;
using System.Net.Http.Json;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Endpoints;

namespace CESI_CI_CD.ApiService.Tests.Endpoints;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_CreatesUser_AndReturnsToken()
    {
        var request = new RegisterRequest($"{Guid.NewGuid()}@collector.shop", "P@ssword123", "Nouveau Vendeur");

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(request.Email, body.Email);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyUsed()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        var request = new RegisterRequest(email, "P@ssword123", "Premier");

        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, request with { DisplayName = "Second" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "password", "Nom")]
    [InlineData("email@test.com", "", "Nom")]
    [InlineData("email@test.com", "password", "")]
    public async Task Register_ReturnsBadRequest_WhenFieldMissing(string email, string password, string displayName)
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, password, displayName));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsToken_WithValidCredentials()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "P@ssword123", "Vendeur"));

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "P@ssword123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserUnknown()
    {
        var response = await _client.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest($"{Guid.NewGuid()}@collector.shop", "whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordWrong()
    {
        var email = $"{Guid.NewGuid()}@collector.shop";
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "P@ssword123", "Vendeur"));

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
