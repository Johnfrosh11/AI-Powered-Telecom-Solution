using FluentValidation;
using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Identity;

namespace NaijaShield.Application.Features.Users;

// ── Get User By ID Query ──────────────────────────────────────────────────────

public record GetUserByIdQuery(Guid UserId, Guid TenantId)
    : IRequest<Result<UserListItemDto>>;

public class GetUserByIdQueryHandler(IUserRepository users)
    : IRequestHandler<GetUserByIdQuery, Result<UserListItemDto>>
{
    public async Task<Result<UserListItemDto>> Handle(GetUserByIdQuery q, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(q.UserId, ct);
        if (user is null || user.TenantId != q.TenantId)
            return Result.Failure<UserListItemDto>("User not found.");

        return Result.Success(new UserListItemDto(
            user.Id, user.Email, user.FullName, user.PreferredLanguage, user.IsActive, user.LastLoginAt,
            user.UserRoles.Select(r => r.Role?.Name ?? string.Empty).ToList(),
            user.TenantId));
    }
}

// ── Update User Profile Command ───────────────────────────────────────────────

public record UpdateUserProfileCommand(
    Guid UserId, Guid TenantId, string FullName, string PreferredLanguage, string TimeZone, string AvatarUrl)
    : IRequest<Result>, ITransactionalCommand;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(100);
    }
}

public class UpdateUserProfileCommandHandler(IUserRepository users)
    : IRequestHandler<UpdateUserProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateUserProfileCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            return Result.Failure("User not found.");

        user.UpdateProfile(cmd.FullName, cmd.PreferredLanguage, cmd.TimeZone, cmd.AvatarUrl);
        return Result.Success();
    }
}

// ── Reactivate User Command ───────────────────────────────────────────────────

public record ReactivateUserCommand(Guid UserId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "users.reactivate";
    public string AuditTargetType => "AppUser";
    public string? AuditTargetId => UserId.ToString();
}

public class ReactivateUserCommandHandler(IUserRepository users)
    : IRequestHandler<ReactivateUserCommand, Result>
{
    public async Task<Result> Handle(ReactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            return Result.Failure("User not found.");

        user.Reactivate();
        return Result.Success();
    }
}
