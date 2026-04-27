using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Features.Fraud;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class FraudController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet("calls")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SearchCalls(
        [FromQuery] string? status, [FromQuery] string? language,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.FraudCallsView))
            return Forbid();
        var tenantId = currentUser.TenantId ?? Guid.Empty;
        return HandleResult(await mediator.Send(
            new SearchScamCallsQuery(tenantId, status, language, from, to, page, pageSize), ct));
    }

    [HttpGet("calls/{id:guid}")]
    public async Task<IActionResult> GetCall(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.FraudCallsView)) return Forbid();
        return HandleNotFound(await mediator.Send(
            new GetScamCallByIdQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("calls/{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.FraudCallsConfirm)) return Forbid();
        return HandleResult(await mediator.Send(
            new ConfirmScamCallCommand(id, currentUser.TenantId ?? Guid.Empty, currentUser.UserId ?? Guid.Empty), ct));
    }

    [HttpPost("calls/{id:guid}/false-positive")]
    public async Task<IActionResult> MarkFalsePositive(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.FraudCallsMarkFalsePositive)) return Forbid();
        return HandleResult(await mediator.Send(
            new MarkFalsePositiveCommand(id, currentUser.TenantId ?? Guid.Empty, currentUser.UserId ?? Guid.Empty), ct));
    }

    [HttpPost("calls/{id:guid}/warn")]
    public async Task<IActionResult> SendWarning(Guid id, [FromBody] SendWarningRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.FraudWarningsSend)) return Forbid();
        return HandleResult(await mediator.Send(
            new SendScamWarningCommand(id, currentUser.TenantId ?? Guid.Empty, req.Channel), ct));
    }

    [HttpGet("patterns")]
    public async Task<IActionResult> GetTopPatterns(CancellationToken ct)
    {
        return Ok(await mediator.Send(
            new NaijaShield.Application.Features.Dashboard.GetTopPatternsQuery(currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("patterns")]
    public async Task<IActionResult> CreatePattern([FromBody] CreatePatternCommand cmd, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.FraudPatternsManage)) return Forbid();
        return HandleResult(await mediator.Send(cmd, ct));
    }

    [HttpGet("watchlist")]
    public async Task<IActionResult> GetWatchlist(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.FraudWatchlistView)) return Forbid();
        return HandleResult(await mediator.Send(
            new GetWatchlistQuery(currentUser.TenantId ?? Guid.Empty, page, pageSize), ct));
    }

    [HttpPost("watchlist/{msisdn}/block")]
    public async Task<IActionResult> BlockNumber(string msisdn, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.FraudWatchlistManage)) return Forbid();
        return HandleResult(await mediator.Send(
            new BlockNumberCommand(msisdn, currentUser.TenantId ?? Guid.Empty), ct));
    }
}

public record SendWarningRequest(NaijaShield.Domain.Enums.WarningChannel Channel);
