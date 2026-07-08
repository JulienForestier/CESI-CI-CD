using Duende.IdentityServer.Models;

namespace CESI_CI_CD.IdentityService;

/// <summary>
/// Configuration statique (client unique, scopes fixes) enregistrée en mémoire — le pattern
/// documenté et recommandé par Duende pour un jeu de clients/scopes petit et non géré
/// dynamiquement (pas besoin de Duende.IdentityServer.EntityFramework ici).
/// </summary>
public static class Config
{
    private static readonly string[] ProfileClaims = ["name"];
    private static readonly string[] RoleClaims = ["role"];

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Email(),
        new IdentityResource("profile", "Profil utilisateur", ProfileClaims),
        new IdentityResource("roles", "Rôles utilisateur", RoleClaims),
    ];

    public static IEnumerable<Client> GetClients(string bffOrigin, string bffClientSecret) =>
    [
        new Client
        {
            ClientId = "collector-shop-bff",
            ClientSecrets = { new Secret(bffClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RedirectUris = { $"{bffOrigin}/signin-oidc" },
            PostLogoutRedirectUris = { $"{bffOrigin}/signout-callback-oidc" },
            AllowOfflineAccess = true,
            AllowedScopes = { "openid", "email", "profile", "roles", "offline_access" },
        },
    ];
}
