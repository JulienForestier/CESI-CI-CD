namespace CESI_CI_CD.ApiService.Endpoints;

/// <summary>
/// Source unique des chemins d'API, utilisée à la fois par l'enregistrement des routes
/// (Map*Endpoints) et par les tests d'intégration, pour éviter les chaînes dupliquées.
/// </summary>
public static class ApiRoutes
{
    public static class Catalog
    {
        public const string Base = "/api";
        public const string Categories = $"{Base}/categories";
        public const string Listings = $"{Base}/listings";
        public const string MyListings = $"{Base}/listings/mine";
        public const string ListingByIdTemplate = $"{Base}/listings/{{id:guid}}";

        public static string ListingById(Guid id) => $"{Base}/listings/{id}";
    }

    public static class Chat
    {
        public const string Base = "/api";
        public const string Conversations = $"{Base}/conversations";
        public const string MessagesTemplate = $"{Conversations}/{{id:guid}}/messages";

        public static string Messages(Guid conversationId) => $"{Conversations}/{conversationId}/messages";
    }

    public static class Moderation
    {
        public const string Base = "/api/admin";
        public const string PendingListings = $"{Base}/listings/pending";
        public const string ApproveTemplate = $"{Base}/listings/{{id:guid}}/approve";
        public const string RejectTemplate = $"{Base}/listings/{{id:guid}}/reject";

        public static string Approve(Guid id) => $"{Base}/listings/{id}/approve";
        public static string Reject(Guid id) => $"{Base}/listings/{id}/reject";
    }

    public static class Interests
    {
        public const string Base = "/api/interests";
    }

    public static class Users
    {
        public const string Base = "/api/users";
        public const string Me = $"{Base}/me";
    }

    public static class Favorites
    {
        public const string Base = "/api";
        public const string List = $"{Base}/favorites";
        public const string FavoriteIds = $"{Base}/favorites/ids";
        public const string ToggleTemplate = $"{Base}/listings/{{id:guid}}/favorite";

        public static string Toggle(Guid listingId) => $"{Base}/listings/{listingId}/favorite";
    }

    public static class Notifications
    {
        public const string Base = "/api/notifications";
        public const string MarkAllRead = $"{Base}/mark-all-read";
    }

    public static class Reports
    {
        public const string ReportTemplate = $"{Catalog.Base}/listings/{{id:guid}}/report";
        public const string AdminList = $"{Moderation.Base}/reports";

        public static string Report(Guid listingId) => $"{Catalog.Base}/listings/{listingId}/report";
    }
}
