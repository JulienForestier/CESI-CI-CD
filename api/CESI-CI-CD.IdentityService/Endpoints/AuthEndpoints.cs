using System.Security.Claims;
using CESI_CI_CD.IdentityService.Contracts;
using CESI_CI_CD.IdentityService.Data;
using CESI_CI_CD.IdentityService.Data.Entities;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.IdentityService.Endpoints;

/// <summary>
/// Endpoints appelés par la petite UI React de login/register (projet séparé, servi en statique
/// par ce service — voir wwwroot). Ce ne sont PAS les Razor Pages du quickstart Duende : on
/// obtient le même résultat (vérifier les identifiants, poser le cookie de session IdentityServer,
/// reprendre le flow OIDC en cours) via IIdentityServerInteractionService + HttpContext.SignInAsync,
/// mais depuis une API JSON classique.
/// </summary>
public static class AuthEndpoints
{
    private const int MinPasswordLength = 8;

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(IdentityRoutes.Account).RequireRateLimiting("auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            HttpContext httpContext,
            IdentityDbContext db,
            IIdentityServerInteractionService interaction) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Results.BadRequest(new { message = "Email, mot de passe et pseudo sont requis." });
            }

            if (request.Password.Length < MinPasswordLength)
            {
                return Results.BadRequest(new { message = $"Le mot de passe doit contenir au moins {MinPasswordLength} caractères." });
            }

            var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return Results.Conflict(new { message = "Un compte existe déjà avec cet email." });
            }

            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                DisplayName = request.DisplayName,
                PasswordHash = string.Empty,
            };
            user.PasswordHash = hasher.HashPassword(user, request.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return await SignInAndResumeAsync(httpContext, interaction, user, request.ReturnUrl);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            HttpContext httpContext,
            IdentityDbContext db,
            IIdentityServerInteractionService interaction) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            return await SignInAndResumeAsync(httpContext, interaction, user, request.ReturnUrl);
        });

        // Cible de UserInteractionOptions.LogoutUrl (voir Program.cs) : /connect/endsession y
        // redirige (GET, logoutId en query) après avoir validé id_token_hint/post_logout_redirect_uri.
        // Pas d'écran de confirmation ici — le clic sur "Se déconnecter" dans l'app est déjà l'action
        // délibérée de l'utilisateur (pattern BFF), donc on signe out et on reprend immédiatement.
        group.MapGet("/logout", async (
            string? logoutId,
            HttpContext httpContext,
            IIdentityServerInteractionService interaction,
            CancellationToken ct) =>
        {
            await httpContext.SignOutAsync();

            var logoutContext = await interaction.GetLogoutContextAsync(logoutId, ct);
            return Results.Redirect(logoutContext.PostLogoutRedirectUri ?? "/");
        });
    }

    private static async Task<IResult> SignInAndResumeAsync(
        HttpContext httpContext,
        IIdentityServerInteractionService interaction,
        User user,
        string? returnUrl)
    {
        // Valide que returnUrl correspond bien à une requête d'autorisation OIDC en cours
        // (protection contre l'open-redirect), comme le fait la page de login standard de Duende.
        var isLocalReturnUrl = string.IsNullOrEmpty(returnUrl) || interaction.IsValidReturnUrl(returnUrl);
        if (!isLocalReturnUrl)
        {
            return Results.BadRequest(new { message = "returnUrl invalide." });
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

        var isUser = new IdentityServerUser(user.Id.ToString())
        {
            DisplayName = user.DisplayName,
            AdditionalClaims = claims,
        };

        await httpContext.SignInAsync(isUser, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
        });

        return Results.Ok(new { returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl });
    }
}
