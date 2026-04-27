using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Dashboard;
using NaijaShield.Application.Features.Conversations;
using NaijaShield.Application.Features.Customers;
using NaijaShield.Application.Features.Reports;
using NaijaShield.Application.Features.AIStudio;
using NaijaShield.Application.Features.Integrations;
using NaijaShield.Application.Features.Audit;
using NaijaShield.Application.Features.Settings;
using NaijaShield.Application.Features.Users;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class DashboardController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis([FromQuery] string range = "today", CancellationToken ct = default) =>
        HandleResult(await mediator.Send(new GetDashboardKpisQuery(currentUser.TenantId ?? Guid.Empty, range), ct));

    [HttpGet("language-distribution")]
    public async Task<IActionResult> GetLanguageDistribution(CancellationToken ct) =>
        HandleResult(await mediator.Send(new GetLanguageDistributionQuery(currentUser.TenantId ?? Guid.Empty), ct));

    [HttpGet("top-patterns")]
    public async Task<IActionResult> GetTopPatterns([FromQuery] int top = 10, CancellationToken ct = default) =>
        HandleResult(await mediator.Send(new GetTopPatternsQuery(currentUser.TenantId ?? Guid.Empty, top), ct));
}

[Authorize]
public class ConversationsController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] string? channel,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        HandleResult(await mediator.Send(
            new ListConversationsQuery(currentUser.TenantId ?? Guid.Empty, status, channel, page, pageSize), ct));

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new GetConversationMessagesQuery(id, currentUser.TenantId ?? Guid.Empty), ct));

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyRequest req, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new ReplyConversationCommand(id, currentUser.TenantId ?? Guid.Empty, currentUser.UserId ?? Guid.Empty, req.Content), ct));

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new CloseConversationCommand(id, currentUser.TenantId ?? Guid.Empty, currentUser.UserId ?? Guid.Empty), ct));
}

public record ReplyRequest(string Content);

[Authorize]
public class CustomersController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? region, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        HandleResult(await mediator.Send(
            new SearchCustomersQuery(currentUser.TenantId ?? Guid.Empty, region, status, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        HandleNotFound(await mediator.Send(
            new GetCustomerQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
}

[Authorize]
public class ReportsController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        HandleResult(await mediator.Send(
            new ListReportsQuery(currentUser.TenantId ?? Guid.Empty, page, pageSize), ct));

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateReportRequest req, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new GenerateReportCommand(currentUser.TenantId ?? Guid.Empty, currentUser.UserId ?? Guid.Empty,
                req.ReportType, req.PeriodStart, req.PeriodEnd), ct));

    [HttpPost("{id:guid}/submit-ncc")]
    public async Task<IActionResult> SubmitToNcc(Guid id, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new SubmitReportToNccCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
}

public record GenerateReportRequest(string ReportType, DateTime PeriodStart, DateTime PeriodEnd);

[Authorize]
public class AIStudioController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet("prompts")]
    public async Task<IActionResult> ListPrompts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        HandleResult(await mediator.Send(
            new ListPromptsQuery(currentUser.TenantId ?? Guid.Empty, page, pageSize), ct));

    [HttpPost("sandbox")]
    public async Task<IActionResult> SandboxTest([FromBody] SandboxTestRequest req, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new SandboxTestCommand(currentUser.TenantId ?? Guid.Empty, req.InputText, req.Language, req.ModelOverride), ct));
}

[Authorize]
public class IntegrationsController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new ListIntegrationsQuery(currentUser.TenantId ?? Guid.Empty), ct));

    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id, [FromBody] ToggleRequest req, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new ToggleIntegrationCommand(id, currentUser.TenantId ?? Guid.Empty, req.Enable), ct));
}

public record ToggleRequest(bool Enable);

[Authorize]
public class AuditController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet("logs")]
    public async Task<IActionResult> List(
        [FromQuery] string? userId, [FromQuery] string? action,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default) =>
        HandleResult(await mediator.Send(
            new ListAuditLogsQuery(currentUser.TenantId ?? Guid.Empty, userId, action, from, to, page, pageSize), ct));
}

[Authorize]
public class SettingsController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new GetTenantSettingsQuery(currentUser.TenantId ?? Guid.Empty), ct));

    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] UpdateTenantSettingsCommand cmd, CancellationToken ct) =>
        HandleResult(await mediator.Send(cmd with { TenantId = currentUser.TenantId ?? Guid.Empty }, ct));
}

[Authorize]
public class UsersController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        HandleResult(await mediator.Send(
            new ListUsersQuery(currentUser.TenantId ?? Guid.Empty, page, pageSize), ct));

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteUserCommand cmd, CancellationToken ct) =>
        HandleResult(await mediator.Send(cmd with { TenantId = currentUser.TenantId ?? Guid.Empty }, ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new DeactivateUserCommand(id, currentUser.TenantId ?? Guid.Empty), ct));

    [HttpPost("{id:guid}/assign-role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest req, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new AssignRoleCommand(id, req.RoleId, currentUser.TenantId ?? Guid.Empty), ct));

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid id, CancellationToken ct) =>
        HandleResult(await mediator.Send(
            new GetUserPermissionsQuery(id), ct));
}

public record AssignRoleRequest(Guid RoleId);
