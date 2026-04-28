using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Features.Dashboard;

// ── Heatmap Data Query ────────────────────────────────────────────────────────

public record GetHeatmapDataQuery(Guid TenantId, string Range = "week")
    : IRequest<Result<IReadOnlyList<HeatmapDataPoint>>>;

public class GetHeatmapDataQueryHandler()
    : IRequestHandler<GetHeatmapDataQuery, Result<IReadOnlyList<HeatmapDataPoint>>>
{
    // Sample data representing Nigerian cities with call density
    public Task<Result<IReadOnlyList<HeatmapDataPoint>>> Handle(GetHeatmapDataQuery q, CancellationToken ct)
    {
        IReadOnlyList<HeatmapDataPoint> seed =
        [
            new("+234803***1234", 142, 6.5244, 3.3792),  // Lagos
            new("+234802***5678", 89, 9.0579, 7.4891),   // Abuja
            new("+234706***9012", 64, 12.0022, 8.5920),  // Kano
            new("+234803***3456", 51, 6.3350, 5.6267),   // Enugu
            new("+234907***7890", 47, 7.3776, 3.9470),   // Ibadan
        ];
        return Task.FromResult(Result.Success(seed));
    }
}

// ── Activity Feed Query ───────────────────────────────────────────────────────

public record GetActivityFeedQuery(Guid TenantId, int Take = 20)
    : IRequest<Result<IReadOnlyList<ActivityFeedItem>>>;

public class GetActivityFeedQueryHandler(IScamCallRepository scamCalls)
    : IRequestHandler<GetActivityFeedQuery, Result<IReadOnlyList<ActivityFeedItem>>>
{
    public async Task<Result<IReadOnlyList<ActivityFeedItem>>> Handle(GetActivityFeedQuery q, CancellationToken ct)
    {
        var calls = await scamCalls.SearchAsync(q.TenantId, null, null, null, null, 1, q.Take, ct);
        var items = calls.Select(c => new ActivityFeedItem(
            c.Id,
            "scam.detected",
            $"Scam call detected from {c.CallerMsisdn[..7]}***",
            c.CreatedAt,
            q.TenantId.ToString(),
            c.DetectedLanguage,
            c.CallerMsisdn.Length > 7 ? c.CallerMsisdn[..7] + "***" : c.CallerMsisdn
        )).ToList();

        return Result.Success<IReadOnlyList<ActivityFeedItem>>(items);
    }
}
