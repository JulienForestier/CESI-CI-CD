namespace CESI_CI_CD.ApiService.Contracts;

public record CreateReportRequest(string Reason, string? Details);

public record ReportResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    Guid ReporterId,
    string ReporterDisplayName,
    string Reason,
    string? Details,
    DateTimeOffset CreatedAt);
