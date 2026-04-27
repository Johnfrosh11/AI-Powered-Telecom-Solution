using NaijaShield.Domain.Common;

namespace NaijaShield.Domain.Aggregates.Identity;

/// <summary>RBAC role scoped to a tenant.</summary>
public class Role : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<RolePermissionAssignment> _rolePermissions = [];
    public IReadOnlyCollection<RolePermissionAssignment> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { }

    public static Role Create(Guid tenantId, string name, string displayName, string description, bool isSystemRole = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            DisplayName = displayName,
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SetPermissions(IEnumerable<Guid> permissionIds)
    {
        _rolePermissions.Clear();
        foreach (var pid in permissionIds)
        {
            _rolePermissions.Add(new RolePermissionAssignment { RoleId = Id, PermissionId = pid });
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string displayName, string description)
    {
        DisplayName = displayName;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>Permission definition (global catalogue).</summary>
public class PermissionEntry : Entity<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>Junction between role and permission.</summary>
public class RolePermissionAssignment
{
    public Guid RoleId { get; init; }
    public Guid PermissionId { get; init; }

    public Role? Role { get; init; }
    public PermissionEntry? PermissionEntry { get; init; }
}
