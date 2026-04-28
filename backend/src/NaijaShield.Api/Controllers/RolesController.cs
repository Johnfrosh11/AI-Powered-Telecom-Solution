using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Roles;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class RolesController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.RolesView)) return Forbid();
        return HandleResult(await mediator.Send(
            new ListRolesQuery(currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.RolesView)) return Forbid();
        return HandleNotFound(await mediator.Send(
            new GetRoleByIdQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleCommand cmd, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.RolesManage)) return Forbid();
        var command = cmd with { TenantId = currentUser.TenantId ?? Guid.Empty };
        return HandleResult(await mediator.Send(command, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.RolesManage)) return Forbid();
        return HandleResult(await mediator.Send(
            new UpdateRoleCommand(id, currentUser.TenantId ?? Guid.Empty, req.DisplayName, req.Description), ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.RolesManage)) return Forbid();
        return HandleResult(await mediator.Send(
            new DeleteRoleCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetRolePermissionsRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.RolesManage)) return Forbid();
        return HandleResult(await mediator.Send(
            new SetRolePermissionsCommand(id, currentUser.TenantId ?? Guid.Empty, req.PermissionIds), ct));
    }
}

public record UpdateRoleRequest(string DisplayName, string Description);
public record SetRolePermissionsRequest(IReadOnlyList<Guid> PermissionIds);
