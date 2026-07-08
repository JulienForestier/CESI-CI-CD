using System.Security.Claims;
using CESI_CI_CD.IdentityService.Data;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.IdentityService;

/// <summary>
/// Recharge les claims depuis la base plutôt que depuis context.Subject : au moment d'un appel
/// à /connect/userinfo (déclenché par GetClaimsFromUserInfoEndpoint côté ApiService), le Subject
/// est reconstruit à partir de l'access token, qui ne porte que "sub" — les claims additionnelles
/// posées à la connexion (AuthEndpoints.SignInAndResumeAsync) n'y sont pas présentes.
/// </summary>
public class CustomProfileService(IdentityDbContext db) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken ct)
    {
        if (!Guid.TryParse(context.Subject.GetSubjectId(), out var userId))
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new("name", user.DisplayName),
            new("email", user.Email),
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim("role", "Admin"));
        }

        context.AddRequestedClaims(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context, CancellationToken ct)
    {
        var userId = Guid.Parse(context.Subject.GetSubjectId());
        context.IsActive = await db.Users.AnyAsync(u => u.Id == userId, ct);
    }
}
