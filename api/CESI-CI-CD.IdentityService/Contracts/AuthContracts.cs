namespace CESI_CI_CD.IdentityService.Contracts;

public record RegisterRequest(string Email, string Password, string DisplayName, string? ReturnUrl);

public record LoginRequest(string Email, string Password, string? ReturnUrl);
