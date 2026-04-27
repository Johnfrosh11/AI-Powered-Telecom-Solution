using NaijaShield.Domain.Aggregates.Identity;

namespace NaijaShield.Application.Common.Interfaces;

/// <summary>Returns the authenticated caller's identity.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
