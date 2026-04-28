using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.AIStudio;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class AiController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet("prompts")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ListPrompts([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.AiPromptsView)) return Forbid();
        return HandleResult(await mediator.Send(
            new ListPromptsQuery(currentUser.TenantId ?? Guid.Empty, page, pageSize), ct));
    }

    [HttpGet("prompts/{id:guid}")]
    public async Task<IActionResult> GetPrompt(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.AiPromptsView)) return Forbid();
        return HandleNotFound(await mediator.Send(
            new GetPromptTemplateByIdQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("prompts")]
    public async Task<IActionResult> CreatePrompt([FromBody] CreatePromptTemplateCommand cmd, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.AiPromptsEdit)) return Forbid();
        var command = cmd with { TenantId = currentUser.TenantId ?? Guid.Empty };
        return HandleResult(await mediator.Send(command, ct));
    }

    [HttpPost("sandbox")]
    public async Task<IActionResult> RunSandbox([FromBody] SandboxTestRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.AiSandboxUse)) return Forbid();
        return HandleResult(await mediator.Send(
            new SandboxTestCommand(currentUser.TenantId ?? Guid.Empty, req.InputText, req.Language, req.ModelOverride), ct));
    }

    [HttpPost("patterns/{id:guid}/train")]
    public async Task<IActionResult> TrainPattern(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.AiModelsRetrain)) return Forbid();
        return HandleResult(await mediator.Send(
            new TrainPatternCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
    }
}

public record RunAiSandboxRequest(string Prompt, string Language);
