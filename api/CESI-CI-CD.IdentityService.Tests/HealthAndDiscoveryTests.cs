using System.Net;
using System.Text.Json;

namespace CESI_CI_CD.IdentityService.Tests;

public class HealthAndDiscoveryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthAndDiscoveryTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", body);
    }

    [Fact]
    public async Task Discovery_ReturnsOpenIdConfiguration_WithExpectedEndpoints()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(json.RootElement.TryGetProperty("authorization_endpoint", out var authEndpoint));
        Assert.Contains("/connect/authorize", authEndpoint.GetString());
        Assert.True(json.RootElement.TryGetProperty("token_endpoint", out var tokenEndpoint));
        Assert.Contains("/connect/token", tokenEndpoint.GetString());
    }
}
