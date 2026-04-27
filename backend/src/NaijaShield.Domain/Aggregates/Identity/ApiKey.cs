using NaijaShield.Domain.Common;

namespace NaijaShield.Domain.Aggregates.Identity;

/// <summary>API key for machine-to-machine auth.</summary>
public class ApiKey : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>Argon2id hash of the raw key.</summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>First 8 characters of the raw key, displayed in listings.</summary>
    public string Prefix { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }

    public string[] Scopes { get; private set; } = [];
    public string[] AllowedIpRanges { get; private set; } = [];

    public DateTime ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    private ApiKey() { }

    public static ApiKey Create(
        Guid tenantId,
        Guid userId,
        string name,
        string keyHash,
        string prefix,
        string[] scopes,
        string[] allowedIpRanges,
        DateTime expiresAt)
    {
        return new ApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            KeyHash = keyHash,
            Prefix = prefix,
            Scopes = scopes,
            AllowedIpRanges = allowedIpRanges,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}

/// <summary>Active session record for force-logout support.</summary>
public class UserSession : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }

    public AppUser? User { get; set; }

    public static UserSession Create(Guid userId, Guid tenantId, string refreshTokenHash, DateTime expiresAt)
    {
        var session = new UserSession
        {
            UserId = userId,
            TenantId = tenantId,
            RefreshTokenHash = refreshTokenHash,
            StartedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        session.Id = Guid.NewGuid();
        return session;
    }
}

/// <summary>Maps an Entra group to a NaijaShield role for automatic SSO provisioning.</summary>
public class SsoGroupMapping : Entity<Guid>
{
    public string EntraGroupId { get; set; } = string.Empty;
    public string EntraGroupName { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>User notification channel preferences.</summary>
public class NotificationPreferences
{
    public Guid UserId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool SlackEnabled { get; set; }
    public string? SlackUserId { get; set; }
    public TimeOnly QuietHoursStart { get; set; } = new(22, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(7, 0);
}
