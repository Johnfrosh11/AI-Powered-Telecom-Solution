using FluentValidation;
using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Constants;

namespace NaijaShield.Application.Features.Users;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record UserListItemDto(
    Guid Id, string Email, string FullName, string PreferredLanguage,
    bool IsActive, DateTime? LastLoginAt, IReadOnlyList<string> Roles, Guid TenantId);

// ── Invite User ───────────────────────────────────────────────────────────────

public record InviteUserCommand(
    string Email,
    string FullName,
    string PreferredLanguage,
    IReadOnlyList<Guid> RoleIds,
    Guid TenantId)
    : IRequest<Result<Guid>>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "users.invite";
    public string AuditTargetType => "AppUser";
    public string? AuditTargetId => null;
}

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreferredLanguage).NotEmpty().Must(l =>
            new[] { "en", "pcm", "yo", "ha", "ig" }.Contains(l))
            .WithMessage("Language must be one of: en, pcm, yo, ha, ig");
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class InviteUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    ICurrentUserService currentUser)
    : IRequestHandler<InviteUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(InviteUserCommand cmd, CancellationToken ct)
    {
        var existing = await users.GetByEmailAsync(cmd.Email, cmd.TenantId, ct);
        if (existing is not null)
            return Result.Failure<Guid>("A user with this email already exists in this tenant.");

        var user = AppUser.Create(cmd.TenantId, cmd.Email, cmd.FullName, cmd.PreferredLanguage);

        foreach (var roleId in cmd.RoleIds)
        {
            var role = await roles.GetByIdAsync(roleId, ct);
            if (role is null || role.TenantId != cmd.TenantId)
                return Result.Failure<Guid>($"Role {roleId} not found.");
            user.AssignRole(roleId, currentUser.UserId ?? Guid.Empty);
        }

        await users.AddAsync(user, ct);
        return Result.Success(user.Id);
    }
}

// ── Deactivate User ───────────────────────────────────────────────────────────

public record DeactivateUserCommand(Guid UserId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "users.deactivate";
    public string AuditTargetType => "AppUser";
    public string? AuditTargetId => UserId.ToString();
    public string AuditSensitivity => "High";
}

public class DeactivateUserCommandHandler(
    IUserRepository users,
    IUserSessionRepository sessions)
    : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            return Result.Failure("User not found.");

        user.Deactivate();
        await sessions.InvalidateUserSessionsAsync(cmd.UserId, ct);
        return Result.Success();
    }
}

// ── Assign Role ───────────────────────────────────────────────────────────────

public record AssignRoleCommand(Guid UserId, Guid RoleId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "roles.assign";
    public string AuditTargetType => "AppUser";
    public string? AuditTargetId => UserId.ToString();
}

public class AssignRoleCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPermissionCache permissionCache,
    ICurrentUserService currentUser)
    : IRequestHandler<AssignRoleCommand, Result>
{
    public async Task<Result> Handle(AssignRoleCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            return Result.Failure("User not found.");

        var role = await roles.GetByIdAsync(cmd.RoleId, ct);
        if (role is null || role.TenantId != cmd.TenantId)
            return Result.Failure("Role not found.");

        user.AssignRole(cmd.RoleId, currentUser.UserId ?? Guid.Empty);
        await permissionCache.InvalidateAsync(cmd.UserId);
        return Result.Success();
    }
}

// ── List Users Query ──────────────────────────────────────────────────────────

public record ListUsersQuery(Guid TenantId, int Page = 1, int PageSize = 50, string? Search = null)
    : IRequest<Result<PagedResult<UserListItemDto>>>;

public class ListUsersQueryHandler(IUserRepository users)
    : IRequestHandler<ListUsersQuery, Result<PagedResult<UserListItemDto>>>
{
    public async Task<Result<PagedResult<UserListItemDto>>> Handle(ListUsersQuery q, CancellationToken ct)
    {
        var items = await users.ListAsync(q.TenantId, q.Page, q.PageSize, q.Search, ct);
        var total = await users.CountAsync(q.TenantId, q.Search, ct);

        var dtos = items.Select(u => new UserListItemDto(
            u.Id, u.Email, u.FullName, u.PreferredLanguage, u.IsActive, u.LastLoginAt,
            u.UserRoles.Select(r => r.Role?.Name ?? string.Empty).ToList(),
            u.TenantId)).ToList();

        return Result.Success(new PagedResult<UserListItemDto>(dtos, total, q.Page, q.PageSize));
    }
}

// ── Get User Permissions Query ────────────────────────────────────────────────

public record GetUserPermissionsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<string>>>;

public class GetUserPermissionsQueryHandler(IPermissionRepository permissions)
    : IRequestHandler<GetUserPermissionsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(GetUserPermissionsQuery q, CancellationToken ct)
    {
        var codes = await permissions.GetPermissionCodesForUserAsync(q.UserId, ct);
        return Result.Success(codes);
    }
}
