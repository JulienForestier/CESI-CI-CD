using System.Security.Claims;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class ClaimsPrincipalExtensions
{
    // "sub" = claim standard OIDC portant l'identifiant du sujet. Remplace l'ancien
    // JwtRegisteredClaimNames.Sub (qui tirait System.IdentityModel.Tokens.Jwt), reliquat de
    // l'authentification JWT désormais retirée au profit du cookie de session émis via le BFF.
    private const string SubjectClaimType = "sub";

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(SubjectClaimType) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}
