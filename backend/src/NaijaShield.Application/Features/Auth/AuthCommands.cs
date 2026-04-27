using FluentValidation;
using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Identity;

namespace NaijaShield.Application.Features.Auth;

// ── DTOs ────────────────────────────────────────────────────────────────────

public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, UserProfileDto User);

public record RefreshRequest(string RefreshToken);
public record RefreshResponse(string AccessToken, string RefreshToken);

public record MfaChallengeResponse(string ChallengeToken);
public record MfaVerifyRequest(string ChallengeToken, string Code);

public record UserProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string PreferredLanguage,
    string TimeZone,
    string AvatarUrl,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

// ── Login Command ────────────────────────────────────────────────────────────

public record LoginCommand(string Email, string Password)
    : IRequest<Result<LoginResponse>>, ITransactionalCommand;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public class LoginCommandHandler(
    IUserRepository users,
    IUserSessionRepository sessions,
    IPermissionRepository permissions,
    ITokenService tokenService,
    IPasswordHasher passwordHasher)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(cmd.Email.ToLowerInvariant(), Guid.Empty, ct);
        if (user is null || !user.IsActive)
            return Result.Failure<LoginResponse>("Invalid email or password.");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !passwordHasher.Verify(cmd.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>("Invalid email or password.");

        var permissionCodes = await permissions.GetPermissionCodesForUserAsync(user.Id, ct);
        var roles = user.UserRoles.Select(r => r.Role?.Name ?? string.Empty).ToList();

        var claims = new TokenClaims(
            user.Id, user.TenantId, user.Email, user.PreferredLanguage,
            roles.AsReadOnly(), permissionCodes);

        var accessToken = tokenService.GenerateAccessToken(claims);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshHash = passwordHasher.Hash(refreshToken);

        var session = UserSession.Create(user.Id, user.TenantId, refreshHash, DateTime.UtcNow.AddDays(7));
        await sessions.AddAsync(session, ct);
        user.RecordLogin();

        var profile = new UserProfileDto(
            user.Id, user.Email, user.FullName,
            user.PreferredLanguage, user.TimeZone, user.AvatarUrl,
            user.TenantId, roles, permissionCodes);

        return Result.Success(new LoginResponse(accessToken, refreshToken, profile));
    }
}

// ── Refresh Command ──────────────────────────────────────────────────────────

public record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<RefreshResponse>>, ITransactionalCommand;

public class RefreshTokenCommandHandler(
    IUserSessionRepository sessions,
    IUserRepository users,
    IPermissionRepository permissions,
    ITokenService tokenService,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RefreshTokenCommand, Result<RefreshResponse>>
{
    public async Task<Result<RefreshResponse>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        // Find matching session by checking all active sessions
        var activeSessions = await sessions.GetActiveSessionsAsync(Guid.Empty, ct);
        UserSession? session = null;
        foreach (var s in activeSessions)
        {
            if (passwordHasher.Verify(cmd.RefreshToken, s.RefreshTokenHash))
            {
                session = s;
                break;
            }
        }

        if (session is null || !session.IsActive || session.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<RefreshResponse>("Invalid or expired refresh token.");

        var user = await users.GetByIdAsync(session.UserId, ct);
        if (user is null || !user.IsActive)
            return Result.Failure<RefreshResponse>("User is inactive.");

        var permissionCodes = await permissions.GetPermissionCodesForUserAsync(user.Id, ct);
        var roles = user.UserRoles.Select(r => r.Role?.Name ?? string.Empty).ToList();

        var claims = new TokenClaims(
            user.Id, user.TenantId, user.Email, user.PreferredLanguage,
            roles.AsReadOnly(), permissionCodes);

        var newAccess = tokenService.GenerateAccessToken(claims);
        var newRefresh = tokenService.GenerateRefreshToken();

        // Rotate refresh token
        session.RefreshTokenHash = passwordHasher.Hash(newRefresh);
        session.LastActiveAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddDays(7);

        return Result.Success(new RefreshResponse(newAccess, newRefresh));
    }
}

// ── Logout Command ───────────────────────────────────────────────────────────

public record LogoutCommand(Guid UserId)
    : IRequest<Result>, ITransactionalCommand;

public class LogoutCommandHandler(IUserSessionRepository sessions)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand cmd, CancellationToken ct)
    {
        await sessions.InvalidateUserSessionsAsync(cmd.UserId, ct);
        return Result.Success();
    }
}

// ── Get Current User Query ───────────────────────────────────────────────────

public record GetCurrentUserQuery(Guid UserId)
    : IRequest<Result<UserProfileDto>>;

public class GetCurrentUserQueryHandler(
    IUserRepository users,
    IPermissionRepository permissions)
    : IRequestHandler<GetCurrentUserQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetCurrentUserQuery q, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(q.UserId, ct);
        if (user is null)
            return Result.Failure<UserProfileDto>("User not found.");

        var permCodes = await permissions.GetPermissionCodesForUserAsync(user.Id, ct);
        var roles = user.UserRoles.Select(r => r.Role?.Name ?? string.Empty).ToList();

        return Result.Success(new UserProfileDto(
            user.Id, user.Email, user.FullName,
            user.PreferredLanguage, user.TimeZone, user.AvatarUrl,
            user.TenantId, roles, permCodes));
    }
}
