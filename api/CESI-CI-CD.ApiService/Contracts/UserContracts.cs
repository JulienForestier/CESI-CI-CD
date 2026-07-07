namespace CESI_CI_CD.ApiService.Contracts;

public record UserProfileResponse(Guid Id, string Email, string DisplayName, bool IsAdmin, DateTimeOffset CreatedAt);

public record UpdateProfileRequest(string DisplayName);
