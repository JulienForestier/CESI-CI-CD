using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CESI_CI_CD.ApiService.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CESI_CI_CD.ApiService.Services;

public class TokenService(IConfiguration configuration)
{
    private const string Issuer = "collector-shop-api";
    private const string Audience = "collector-shop-app";
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public string GenerateToken(User user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Key' manquante.");

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("displayName", user.DisplayName),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(Lifetime),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
