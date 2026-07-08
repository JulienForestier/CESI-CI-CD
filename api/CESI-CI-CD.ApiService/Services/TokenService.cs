using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CESI_CI_CD.ApiService.Configuration;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CESI_CI_CD.ApiService.Services;

public class TokenService(IConfiguration configuration)
{
    [SuppressMessage("Security", "S6781:JWT secret keys should not be disclosed",
        Justification = "La clé provient exclusivement d'une variable d'environnement injectée par un Sealed Secret Kubernetes (Jwt__Key) — jamais codée en dur ni stockée dans appsettings.json, et le démarrage échoue explicitement si elle est absente.")]
    public string GenerateToken(User user)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Configuration '{JwtOptions.SectionName}' manquante.");

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("displayName", user.DisplayName),
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(jwtOptions.LifetimeDays),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
