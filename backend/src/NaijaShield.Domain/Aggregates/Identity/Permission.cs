using NaijaShield.Domain.Common;

namespace NaijaShield.Domain.Aggregates.Identity;

/// <summary>Permission definition entry in the global permission catalogue.</summary>
public class PermissionEntry : Entity<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>Junction between a <see cref="Role"/> and a <see cref="PermissionEntry"/>.</summary>
public class RolePermissionAssignment
{
    public Guid RoleId { get; init; }
    public Guid PermissionId { get; init; }

    public Role? Role { get; init; }
    public PermissionEntry? PermissionEntry { get; init; }
}
