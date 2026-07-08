namespace CESI_CI_CD.ApiService.Contracts;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, Guid UserId, string Email, string DisplayName, bool IsAdmin);
