using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Reports;
using NaijaShield.Domain.Constants;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class ReportsController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.ReportsView)) return Forbid();
        return HandleResult(await mediator.Send(
            new ListReportsQuery(currentUser.TenantId ?? Guid.Empty, page, pageSize), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.ReportsView)) return Forbid();
        return HandleNotFound(await mediator.Send(
            new GetReportByIdQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] GenerateReportCommand cmd, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.ReportsCreate)) return Forbid();
        var command = cmd with
        {
            TenantId = currentUser.TenantId ?? Guid.Empty,
            RequestedByUserId = currentUser.UserId ?? Guid.Empty
        };
        return HandleResult(await mediator.Send(command, ct));
    }

    [HttpPost("{id:guid}/submit-ncc")]
    public async Task<IActionResult> SubmitNcc(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.ReportsSubmitNcc)) return Forbid();
        return HandleResult(await mediator.Send(
            new SubmitReportToNccCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("{id:guid}/submit-efcc")]
    public async Task<IActionResult> SubmitEfcc(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.FraudReportsSubmitEfcc)) return Forbid();
        return HandleResult(await mediator.Send(
            new SubmitReportToEfccCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
    }
}
