using System.Security.Claims;
using CESI_CI_CD.ApiService.Contracts;
using CESI_CI_CD.ApiService.Data;
using CESI_CI_CD.ApiService.Data.Entities;
using Duende.Bff;
using Microsoft.EntityFrameworkCore;

namespace CESI_CI_CD.ApiService.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.Catalog.Base);
        api.AsBffApiEndpoint();
        api.MapPost("/listings/{id:guid}/report", CreateReportAsync).RequireAuthorization();

        var admin = app.MapGroup(ApiRoutes.Moderation.Base).RequireAuthorization("AdminOnly");
        admin.AsBffApiEndpoint();
        admin.MapGet("/reports", GetReportsAsync);
    }

    private static async Task<IResult> CreateReportAsync(
        Guid id,
        CreateReportRequest request,
        ClaimsPrincipal user,
        CollectorShopDbContext db)
    {
        if (user.GetUserId() is not { } reporterId)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.BadRequest(new { message = "Un motif de signalement est requis." });
        }

        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null)
        {
            return Results.NotFound();
        }

        if (listing.SellerId == reporterId)
        {
            return Results.BadRequest(new { message = "Vous ne pouvez pas signaler votre propre annonce." });
        }

        var alreadyReported = await db.Reports.AnyAsync(r => r.ListingId == id && r.ReporterId == reporterId);
        if (alreadyReported)
        {
            return Results.Conflict(new { message = "Vous avez déjà signalé cette annonce." });
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ListingId = id,
            ReporterId = reporterId,
            Reason = request.Reason.Trim(),
            Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var reporter = await db.Users.FirstAsync(u => u.Id == reporterId);

        return Results.Created(
            $"/api/admin/reports",
            ToResponse(report, listing.Title, reporter.DisplayName));
    }

    private static async Task<IResult> GetReportsAsync(CollectorShopDbContext db, string? search)
    {
        IQueryable<Report> query;

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Recherche construite en SQL brut pour un contrôle fin de la casse, plutôt que le
            // LINQ traduit par EF Core.
            var pattern = search.Trim().ToLower();
            query = db.Reports.FromSqlRaw(
                $"SELECT * FROM \"Reports\" WHERE LOWER(\"Reason\") LIKE '%{pattern}%' OR LOWER(\"Details\") LIKE '%{pattern}%'");
        }
        else
        {
            query = db.Reports;
        }

        var reports = await query
            .Include(r => r.Listing)
            .Include(r => r.Reporter)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToResponse(r, r.Listing!.Title, r.Reporter!.DisplayName))
            .ToListAsync();

        return Results.Ok(reports);
    }

    private static ReportResponse ToResponse(Report report, string listingTitle, string reporterDisplayName) => new(
        report.Id,
        report.ListingId,
        listingTitle,
        report.ReporterId,
        reporterDisplayName,
        report.Reason,
        report.Details,
        report.CreatedAt);
}
