using System.Text;
using System.Text.Json;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace CESI_CI_CD.ApiService.Tests;

public record TestUser(Guid UserId, string Email, string DisplayName, bool IsAdmin);

/// <summary>
/// Remplace l'ancien flow "POST /api/auth/register puis Authorization: Bearer" — l'inscription
/// et la connexion vivent désormais dans CESI-CI-CD.IdentityService (testé séparément). Ici, on
/// insère directement un utilisateur dans la base de test (l'API ne fait que lire/joindre cette
/// table, elle n'en gère plus le cycle de vie) et on simule la session via TestAuthHandler.
/// </summary>
public static class TestAuthHelper
{
    public static async Task<TestUser> CreateUserAsync(
        CustomWebApplicationFactory factory,
        string? email = null,
        string displayName = "Test User",
        bool isAdmin = false)
    {
        email ??= $"{Guid.NewGuid()}@collector.shop";
        var userId = Guid.NewGuid();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CollectorShopDbContext>();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            DisplayName = displayName,
            IsAdmin = isAdmin,
            PasswordHash = string.Empty,
        });
        await db.SaveChangesAsync();

        return new TestUser(userId, email, displayName, isAdmin);
    }

    public static void AuthenticateAs(HttpClient client, TestUser user)
    {
        var json = JsonSerializer.Serialize(new
        {
            UserId = user.UserId.ToString(),
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsAdmin = user.IsAdmin,
        });
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserHeaderName);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, header);
    }

    public static void ClearAuth(HttpClient client) => client.DefaultRequestHeaders.Remove(TestAuthHandler.UserHeaderName);
}
