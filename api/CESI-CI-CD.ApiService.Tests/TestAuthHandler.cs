using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CESI_CI_CD.ApiService.Tests;

/// <summary>
/// Schéma d'authentification de test — remplace le scheme cookie/Duende.BFF réel de l'API.
/// En production, l'IdentityService (Duende IdentityServer, projet séparé) valide les
/// identifiants ; ici, on court-circuite tout le flow OIDC et on construit directement le
/// ClaimsPrincipal à partir d'un header, pour tester les endpoints métier de l'API isolément.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeaderName = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeaderName, out var header) || string.IsNullOrEmpty(header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(header!));
        var payload = JsonSerializer.Deserialize<TestUserPayload>(json)
            ?? throw new InvalidOperationException($"En-tête {UserHeaderName} invalide.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, payload.UserId),
            new("email", payload.Email),
            new("name", payload.DisplayName),
        };

        if (payload.IsAdmin)
        {
            claims.Add(new Claim("role", "Admin"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName, nameType: "name", roleType: "role");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private record TestUserPayload(string UserId, string Email, string DisplayName, bool IsAdmin);
}
