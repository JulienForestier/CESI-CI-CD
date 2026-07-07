using System.IdentityModel.Tokens.Jwt;
using CESI_CI_CD.ApiService.Data.Entities;
using CESI_CI_CD.ApiService.Services;
using Microsoft.Extensions.Configuration;

namespace CESI_CI_CD.ApiService.Tests.Services;

public class TokenServiceTests
{
    private static TokenService CreateSut()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-0000000000000000000000",
            })
            .Build();

        return new TokenService(configuration);
    }

    [Fact]
    public void GenerateToken_ProducesTokenWithExpectedClaims()
    {
        var sut = CreateSut();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller@collector.shop",
            DisplayName = "Seller One",
            PasswordHash = "hash",
        };

        var token = sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("collector-shop-api", jwt.Issuer);
        Assert.Contains("collector-shop-app", jwt.Audiences);
        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.DisplayName, jwt.Claims.First(c => c.Type == "displayName").Value);
    }

    [Fact]
    public void GenerateToken_Throws_WhenKeyMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new TokenService(configuration);
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", DisplayName = "A", PasswordHash = "h" };

        Assert.Throws<InvalidOperationException>(() => sut.GenerateToken(user));
    }
}
