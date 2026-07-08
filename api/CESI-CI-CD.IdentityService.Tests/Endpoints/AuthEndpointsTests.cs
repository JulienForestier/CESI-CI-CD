using System.Net;
using System.Net.Http.Json;
using CESI_CI_CD.IdentityService.Contracts;
using CESI_CI_CD.IdentityService.Endpoints;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CESI_CI_CD.IdentityService.Tests.Endpoints;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string UniqueEmail() => $"{Guid.NewGuid()}@collector.shop";

    [Fact]
    public async Task Register_CreatesUser_AndReturnsRoot_WhenNoReturnUrl()
    {
        var response = await _client.PostAsJsonAsync(
            IdentityRoutes.Register,
            new RegisterRequest(UniqueEmail(), "P@ssword123", "Utilisateur Test", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReturnUrlResponse>();
        Assert.Equal("/", body!.ReturnUrl);
    }

    [Theory]
    [InlineData("", "P@ssword123", "Utilisateur")]
    [InlineData("a@b.com", "", "Utilisateur")]
    [InlineData("a@b.com", "P@ssword123", "")]
    public async Task Register_ReturnsBadRequest_WhenFieldMissing(string email, string password, string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            IdentityRoutes.Register,
            new RegisterRequest(email, password, displayName, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordTooShort()
    {
        var response = await _client.PostAsJsonAsync(
            IdentityRoutes.Register,
            new RegisterRequest(UniqueEmail(), "short", "Utilisateur Test", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync(IdentityRoutes.Register, new RegisterRequest(email, "P@ssword123", "Premier", null));

        var response = await _client.PostAsJsonAsync(
            IdentityRoutes.Register,
            new RegisterRequest(email, "P@ssword123", "Deuxième", null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenReturnUrlIsNotAPendingAuthorizeRequest()
    {
        var response = await _client.PostAsJsonAsync(
            IdentityRoutes.Register,
            new RegisterRequest(UniqueEmail(), "P@ssword123", "Utilisateur Test", "/not-a-real-oidc-callback"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailUnknown()
    {
        var response = await _client.PostAsJsonAsync(
            IdentityRoutes.Login,
            new LoginRequest(UniqueEmail(), "P@ssword123", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordWrong()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync(IdentityRoutes.Register, new RegisterRequest(email, "P@ssword123", "Utilisateur Test", null));

        var response = await _client.PostAsJsonAsync(IdentityRoutes.Login, new LoginRequest(email, "WrongPassword1", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithValidCredentials()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync(IdentityRoutes.Register, new RegisterRequest(email, "P@ssword123", "Utilisateur Test", null));

        var response = await _client.PostAsJsonAsync(IdentityRoutes.Login, new LoginRequest(email, "P@ssword123", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReturnUrlResponse>();
        Assert.Equal("/", body!.ReturnUrl);
    }

    [Fact]
    public async Task Logout_SignsOutAndRedirects_WhenNoActiveLogoutContext()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(IdentityRoutes.Logout);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private sealed record ReturnUrlResponse(string ReturnUrl);
}
