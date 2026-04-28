using Microsoft.AspNetCore.Authorization;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Api.Authorization;

/// <summary>
/// Requirement that checks a single permission code (e.g. "fraud.calls.view").
/// </summary>
public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}

/// <summary>
/// Evaluates <see cref="PermissionRequirement"/> by delegating to <see cref="ICurrentUserService"/>.
/// Works for both JWT and API-key authenticated requests.
/// </summary>
public sealed class PermissionAuthorizationHandler(ICurrentUserService currentUser)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (currentUser.HasPermission(requirement.PermissionCode))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
