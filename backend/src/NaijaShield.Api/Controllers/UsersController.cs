using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Users;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class UsersController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.UsersView)) return Forbid();
        return HandleResult(await mediator.Send(
            new ListUsersQuery(currentUser.TenantId ?? Guid.Empty, page, pageSize, search), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.UsersView)) return Forbid();
        return HandleNotFound(await mediator.Send(
            new GetUserByIdQuery(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteUserCommand cmd, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.UsersInvite)) return Forbid();
        var command = cmd with { TenantId = currentUser.TenantId ?? Guid.Empty };
        return HandleResult(await mediator.Send(command, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateUserProfileRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.UsersInvite)) return Forbid();
        return HandleResult(await mediator.Send(
            new UpdateUserProfileCommand(id, currentUser.TenantId ?? Guid.Empty, req.FullName, req.PreferredLanguage, req.TimeZone, req.AvatarUrl), ct));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.UsersDeactivate)) return Forbid();
        return HandleResult(await mediator.Send(
            new DeactivateUserCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.UsersDeactivate)) return Forbid();
        return HandleResult(await mediator.Send(
            new ReactivateUserCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest req, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.RolesManage)) return Forbid();
        // Assign each role individually
        foreach (var roleId in req.RoleIds)
        {
            var result = await mediator.Send(
                new AssignRoleCommand(id, roleId, currentUser.TenantId ?? Guid.Empty), ct);
            if (result.IsFailure) return HandleResult(result);
        }
        return NoContent();
    }
}

public record UpdateUserProfileRequest(string FullName, string PreferredLanguage, string TimeZone, string AvatarUrl);
public record AssignRolesRequest(IReadOnlyList<Guid> RoleIds);
