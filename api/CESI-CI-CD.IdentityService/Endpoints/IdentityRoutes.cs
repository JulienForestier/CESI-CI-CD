namespace CESI_CI_CD.IdentityService.Endpoints;

/// <summary>
/// Source unique des chemins de l'IdentityService, utilisée à la fois par l'enregistrement
/// des routes et par les tests d'intégration.
/// </summary>
public static class IdentityRoutes
{
    public const string Account = "/account";
    public const string Register = $"{Account}/register";
    public const string Login = $"{Account}/login";
}
