using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Audit;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class AuditController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List(
        [FromQuery] string? action, [FromQuery] string? userId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.AuditView)) return Forbid();
        return HandleResult(await mediator.Send(
            new ListAuditLogsQuery(currentUser.TenantId ?? Guid.Empty, userId, action, from, to, page, pageSize), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.AuditView)) return Forbid();
        return HandleNotFound(await mediator.Send(
            new GetAuditLogByIdQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ExportAuditLogsRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.AuditExport)) return Forbid();
        return HandleResult(await mediator.Send(
            new ExportAuditLogsCommand(
                currentUser.TenantId ?? Guid.Empty,
                req.From,
                req.To,
                currentUser.UserId ?? Guid.Empty), ct));
    }
}

public record ExportAuditLogsRequest(DateTime From, DateTime To);
