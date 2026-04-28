using FluentValidation;
using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Identity;

namespace NaijaShield.Application.Features.Roles;

// ── List Roles ────────────────────────────────────────────────────────────────

public record ListRolesQuery(Guid TenantId)
    : IRequest<Result<IReadOnlyList<RoleDto>>>;

public class ListRolesQueryHandler(IRoleRepository roles)
    : IRequestHandler<ListRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(ListRolesQuery q, CancellationToken ct)
    {
        var list = await roles.ListAsync(q.TenantId, ct);
        var dtos = list.Select(r => new RoleDto(
            r.Id, r.Name, r.DisplayName, r.Description, r.IsSystemRole,
            r.RolePermissions.Select(p => p.PermissionId).ToList(),
            r.TenantId)).ToList();

        return Result.Success<IReadOnlyList<RoleDto>>(dtos);
    }
}

// ── Get Role By ID ────────────────────────────────────────────────────────────

public record GetRoleByIdQuery(Guid RoleId, Guid TenantId)
    : IRequest<Result<RoleDto>>;

public class GetRoleByIdQueryHandler(IRoleRepository roles)
    : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery q, CancellationToken ct)
    {
        var r = await roles.GetByIdAsync(q.RoleId, ct);
        if (r is null || r.TenantId != q.TenantId)
            return Result.Failure<RoleDto>("Role not found.");

        return Result.Success(new RoleDto(
            r.Id, r.Name, r.DisplayName, r.Description, r.IsSystemRole,
            r.RolePermissions.Select(p => p.PermissionId).ToList(),
            r.TenantId));
    }
}

// ── Create Role ───────────────────────────────────────────────────────────────

public record CreateRoleCommand(Guid TenantId, string Name, string DisplayName, string Description)
    : IRequest<Result<Guid>>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "roles.created";
    public string AuditTargetType => "Role";
    public string? AuditTargetId => null;
}

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public class CreateRoleCommandHandler(IRoleRepository roles)
    : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand cmd, CancellationToken ct)
    {
        var existing = await roles.GetByNameAsync(cmd.Name, cmd.TenantId, ct);
        if (existing is not null)
            return Result.Failure<Guid>("A role with that name already exists.");

        var role = Role.Create(cmd.TenantId, cmd.Name, cmd.DisplayName, cmd.Description);
        await roles.AddAsync(role, ct);
        return Result.Success(role.Id);
    }
}

// ── Update Role ───────────────────────────────────────────────────────────────

public record UpdateRoleCommand(Guid RoleId, Guid TenantId, string DisplayName, string Description)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "roles.updated";
    public string AuditTargetType => "Role";
    public string? AuditTargetId => RoleId.ToString();
}

public class UpdateRoleCommandHandler(IRoleRepository roles)
    : IRequestHandler<UpdateRoleCommand, Result>
{
    public async Task<Result> Handle(UpdateRoleCommand cmd, CancellationToken ct)
    {
        var r = await roles.GetByIdAsync(cmd.RoleId, ct);
        if (r is null || r.TenantId != cmd.TenantId)
            return Result.Failure("Role not found.");

        if (r.IsSystemRole)
            return Result.Failure("System roles cannot be modified.");

        r.Update(cmd.DisplayName, cmd.Description);
        return Result.Success();
    }
}

// ── Delete Role ───────────────────────────────────────────────────────────────

public record DeleteRoleCommand(Guid RoleId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "roles.deleted";
    public string AuditTargetType => "Role";
    public string? AuditTargetId => RoleId.ToString();
}

public class DeleteRoleCommandHandler(IRoleRepository roles)
    : IRequestHandler<DeleteRoleCommand, Result>
{
    public async Task<Result> Handle(DeleteRoleCommand cmd, CancellationToken ct)
    {
        var r = await roles.GetByIdAsync(cmd.RoleId, ct);
        if (r is null || r.TenantId != cmd.TenantId)
            return Result.Failure("Role not found.");

        if (r.IsSystemRole)
            return Result.Failure("System roles cannot be deleted.");

        roles.Delete(r);
        return Result.Success();
    }
}

// ── Set Role Permissions ──────────────────────────────────────────────────────

public record SetRolePermissionsCommand(Guid RoleId, Guid TenantId, IEnumerable<Guid> PermissionIds)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "roles.permissions_set";
    public string AuditTargetType => "Role";
    public string? AuditTargetId => RoleId.ToString();
}

public class SetRolePermissionsCommandHandler(IRoleRepository roles)
    : IRequestHandler<SetRolePermissionsCommand, Result>
{
    public async Task<Result> Handle(SetRolePermissionsCommand cmd, CancellationToken ct)
    {
        var r = await roles.GetByIdAsync(cmd.RoleId, ct);
        if (r is null || r.TenantId != cmd.TenantId)
            return Result.Failure("Role not found.");

        r.SetPermissions(cmd.PermissionIds);
        return Result.Success();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record RoleDto(
    Guid Id, string Name, string DisplayName, string Description,
    bool IsSystemRole, IReadOnlyList<Guid> PermissionIds, Guid TenantId);
