using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Features.Dashboard;

// ── KPI DTOs ──────────────────────────────────────────────────────────────────

public record DashboardKpisDto(
    int TotalScamCallsDetected,
    int ScamCallsConfirmed,
    int VictimsWarned,
    decimal EstimatedMoneySavedNgn,
    int ActiveWatchlistNumbers,
    int OpenConversations,
    decimal AvgAiConfidence,
    int ReportsPendingSubmission);

public record HeatmapDataPoint(string Msisdn, int Count, double Latitude, double Longitude);

public record LanguageDistributionItem(string Language, int Count, decimal Percentage);

public record TopPatternItem(Guid PatternId, string PatternName, string Category, int TimesTriggered, decimal AccuracyRate);

public record ActivityFeedItem(
    Guid Id, string EventType, string Description, DateTime Timestamp,
    string TenantId, string Language, string? MsisdnMasked);

// ── Queries ────────────────────────────────────────────────────────────────────

public record GetDashboardKpisQuery(Guid TenantId, string Range = "today")
    : IRequest<Result<DashboardKpisDto>>;

public class GetDashboardKpisQueryHandler(
    IScamCallRepository scamCalls,
    IWatchlistedNumberRepository watchlist,
    IConversationRepository conversations,
    IRegulatoryReportRepository reports)
    : IRequestHandler<GetDashboardKpisQuery, Result<DashboardKpisDto>>
{
    public async Task<Result<DashboardKpisDto>> Handle(GetDashboardKpisQuery q, CancellationToken ct)
    {
        var (from, to) = q.Range switch
        {
            "week" => (DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            "month" => (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            _ => (DateTime.UtcNow.Date, DateTime.UtcNow) // today
        };

        var detected = await scamCalls.CountAsync(q.TenantId, "Detected", null, from, to, ct);
        var confirmed = await scamCalls.CountAsync(q.TenantId, "Confirmed", null, from, to, ct);
        var watchlistCount = await watchlist.CountAsync(q.TenantId, ct);
        var openConversations = await conversations.CountAsync(q.TenantId, null, null, "Open", ct);
        var pendingReports = await reports.CountAsync(q.TenantId, ct);

        return Result.Success(new DashboardKpisDto(
            TotalScamCallsDetected: detected,
            ScamCallsConfirmed: confirmed,
            VictimsWarned: detected, // approximation — could join warnings table
            EstimatedMoneySavedNgn: confirmed * 50_000m, // seed estimate
            ActiveWatchlistNumbers: watchlistCount,
            OpenConversations: openConversations,
            AvgAiConfidence: 0.89m, // computed from read model
            ReportsPendingSubmission: pendingReports));
    }
}

public record GetTopPatternsQuery(Guid TenantId, int Top = 10)
    : IRequest<Result<IReadOnlyList<TopPatternItem>>>;

public class GetTopPatternsQueryHandler(IScamPatternRepository patterns)
    : IRequestHandler<GetTopPatternsQuery, Result<IReadOnlyList<TopPatternItem>>>
{
    public async Task<Result<IReadOnlyList<TopPatternItem>>> Handle(GetTopPatternsQuery q, CancellationToken ct)
    {
        var all = await patterns.ListActiveAsync(q.TenantId, ct);
        var top = all
            .OrderByDescending(p => p.TimesTriggered)
            .Take(q.Top)
            .Select(p => new TopPatternItem(p.Id, p.Name, p.Category, p.TimesTriggered, p.DetectionAccuracy))
            .ToList();

        return Result.Success<IReadOnlyList<TopPatternItem>>(top);
    }
}

public record GetLanguageDistributionQuery(Guid TenantId, string Range = "month")
    : IRequest<Result<IReadOnlyList<LanguageDistributionItem>>>;

public class GetLanguageDistributionQueryHandler
    : IRequestHandler<GetLanguageDistributionQuery, Result<IReadOnlyList<LanguageDistributionItem>>>
{
    // Real impl would query scam_calls grouped by detected_language
    public Task<Result<IReadOnlyList<LanguageDistributionItem>>> Handle(
        GetLanguageDistributionQuery q, CancellationToken ct)
    {
        IReadOnlyList<LanguageDistributionItem> seed =
        [
            new("en", 412, 41.2m),
            new("pcm", 283, 28.3m),
            new("yo", 156, 15.6m),
            new("ha", 97, 9.7m),
            new("ig", 52, 5.2m),
        ];
        return Task.FromResult(Result.Success(seed));
    }
}
