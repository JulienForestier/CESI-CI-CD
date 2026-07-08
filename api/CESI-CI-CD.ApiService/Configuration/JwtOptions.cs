namespace CESI_CI_CD.ApiService.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Key { get; init; }
    public string Issuer { get; init; } = "collector-shop-api";
    public string Audience { get; init; } = "collector-shop-app";
    public int LifetimeDays { get; init; } = 7;
}
