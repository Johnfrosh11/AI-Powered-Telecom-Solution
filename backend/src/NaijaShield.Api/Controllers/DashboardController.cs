using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Dashboard;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class DashboardController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet("kpis")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetKpis([FromQuery] string range = "today", CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId ?? Guid.Empty;
        return HandleResult(await mediator.Send(new GetDashboardKpisQuery(tenantId, range), ct));
    }

    [HttpGet("top-patterns")]
    public async Task<IActionResult> GetTopPatterns([FromQuery] int top = 10, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId ?? Guid.Empty;
        return HandleResult(await mediator.Send(new GetTopPatternsQuery(tenantId, top), ct));
    }

    [HttpGet("language-distribution")]
    public async Task<IActionResult> GetLanguageDistribution([FromQuery] string range = "month", CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId ?? Guid.Empty;
        return HandleResult(await mediator.Send(new GetLanguageDistributionQuery(tenantId, range), ct));
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] string range = "week", CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId ?? Guid.Empty;
        return HandleResult(await mediator.Send(new GetHeatmapDataQuery(tenantId, range), ct));
    }

    [HttpGet("activity-feed")]
    public async Task<IActionResult> GetActivityFeed([FromQuery] int take = 20, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId ?? Guid.Empty;
        return HandleResult(await mediator.Send(new GetActivityFeedQuery(tenantId, take), ct));
    }
}
