using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Conversations;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class ConversationsController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] string? language,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.ConversationsView)) return Forbid();
        return HandleResult(await mediator.Send(
            new ListConversationsQuery(currentUser.TenantId ?? Guid.Empty, status, language, page, pageSize), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.ConversationsView)) return Forbid();
        return HandleNotFound(await mediator.Send(
            new GetConversationByIdQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.ConversationsTakeover)) return Forbid();
        return HandleResult(await mediator.Send(
            new ReplyConversationCommand(id, currentUser.TenantId ?? Guid.Empty, currentUser.UserId ?? Guid.Empty, req.Content), ct));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.ConversationsClose)) return Forbid();
        return HandleResult(await mediator.Send(
            new CloseConversationCommand(id, currentUser.TenantId ?? Guid.Empty, currentUser.UserId ?? Guid.Empty), ct));
    }

    [HttpPost("{id:guid}/escalate")]
    public async Task<IActionResult> Escalate(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.ConversationsTakeover)) return Forbid();
        return HandleResult(await mediator.Send(
            new EscalateConversationCommand(id, currentUser.TenantId ?? Guid.Empty, string.Empty), ct));
    }
}

public record ReplyRequest(string Content);
