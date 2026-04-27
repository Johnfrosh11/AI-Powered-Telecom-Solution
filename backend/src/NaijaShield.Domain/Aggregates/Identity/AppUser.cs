using NaijaShield.Domain.Common;

namespace NaijaShield.Domain.Aggregates.Identity;

/// <summary>Application user — federated via Entra ID or local seed account.</summary>
public class AppUser : AggregateRoot<Guid>
{
    public string EntraObjectId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string PreferredLanguage { get; private set; } = "en";
    public string TimeZone { get; private set; } = "Africa/Lagos";
    public string AvatarUrl { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? TotpSecretHash { get; private set; }

    public Guid TenantId { get; private set; }

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public NotificationPreferences? NotificationPrefs { get; private set; }

    private AppUser() { }

    public static AppUser Create(
        Guid tenantId,
        string email,
        string fullName,
        string preferredLanguage = "en",
        string? entraObjectId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.ToLowerInvariant(),
            FullName = fullName,
            PreferredLanguage = preferredLanguage,
            EntraObjectId = entraObjectId ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SetPasswordHash(string hash)
    {
        PasswordHash = hash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string preferredLanguage, string timeZone, string avatarUrl)
    {
        FullName = fullName;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableMfa(string totpSecretHash)
    {
        MfaEnabled = true;
        TotpSecretHash = totpSecretHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableMfa()
    {
        MfaEnabled = false;
        TotpSecretHash = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignRole(Guid roleId, Guid assignedBy)
    {
        if (_userRoles.Any(r => r.RoleId == roleId)) return;
        _userRoles.Add(new UserRole { UserId = Id, RoleId = roleId, AssignedAt = DateTime.UtcNow, AssignedBy = assignedBy });
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveRole(Guid roleId)
    {
        var existing = _userRoles.FirstOrDefault(r => r.RoleId == roleId);
        if (existing is not null)
        {
            _userRoles.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>Junction between user and role.</summary>
public class UserRole
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public DateTime AssignedAt { get; init; }
    public Guid AssignedBy { get; init; }

    public AppUser? User { get; init; }
    public Role? Role { get; init; }
}
