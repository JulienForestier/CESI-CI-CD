using System.Security.Claims;
using CESI_CI_CD.IdentityService.Data;
using CESI_CI_CD.IdentityService.Data.Entities;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.IdentityService.Tests;

public class CustomProfileServiceTests
{
    private static IdentityDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private static ClaimsPrincipal PrincipalFor(Guid userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString())], "test"));

    [Fact]
    public async Task GetProfileDataAsync_AddsNameEmailAndRoleClaims_ForAdmin()
    {
        await using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), Email = "admin@collector.shop", DisplayName = "Admin", PasswordHash = "x", IsAdmin = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new CustomProfileService(db);
        var context = new ProfileDataRequestContext
        {
            Subject = PrincipalFor(user.Id),
            RequestedClaimTypes = ["name", "email", "role"],
            IssuedClaims = [],
        };

        await service.GetProfileDataAsync(context, CancellationToken.None);

        Assert.Contains(context.IssuedClaims, c => c.Type == "name" && c.Value == "Admin");
        Assert.Contains(context.IssuedClaims, c => c.Type == "email" && c.Value == "admin@collector.shop");
        Assert.Contains(context.IssuedClaims, c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public async Task GetProfileDataAsync_DoesNotAddRoleClaim_ForNonAdmin()
    {
        await using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), Email = "user@collector.shop", DisplayName = "User", PasswordHash = "x", IsAdmin = false };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new CustomProfileService(db);
        var context = new ProfileDataRequestContext
        {
            Subject = PrincipalFor(user.Id),
            RequestedClaimTypes = ["name", "email", "role"],
            IssuedClaims = [],
        };

        await service.GetProfileDataAsync(context, CancellationToken.None);

        Assert.DoesNotContain(context.IssuedClaims, c => c.Type == "role");
    }

    [Fact]
    public async Task GetProfileDataAsync_AddsNoClaims_WhenUserNoLongerExists()
    {
        await using var db = CreateDb();
        var service = new CustomProfileService(db);
        var context = new ProfileDataRequestContext
        {
            Subject = PrincipalFor(Guid.NewGuid()),
            RequestedClaimTypes = ["name", "email"],
            IssuedClaims = [],
        };

        await service.GetProfileDataAsync(context, CancellationToken.None);

        Assert.Empty(context.IssuedClaims);
    }

    [Fact]
    public async Task IsActiveAsync_IsTrue_WhenUserExists()
    {
        await using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), Email = "user@collector.shop", DisplayName = "User", PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new CustomProfileService(db);
        var context = new IsActiveContext(PrincipalFor(user.Id), new Client(), "test");

        await service.IsActiveAsync(context, CancellationToken.None);

        Assert.True(context.IsActive);
    }

    [Fact]
    public async Task IsActiveAsync_IsFalse_WhenUserDeleted()
    {
        await using var db = CreateDb();
        var service = new CustomProfileService(db);
        var context = new IsActiveContext(PrincipalFor(Guid.NewGuid()), new Client(), "test");

        await service.IsActiveAsync(context, CancellationToken.None);

        Assert.False(context.IsActive);
    }
}
