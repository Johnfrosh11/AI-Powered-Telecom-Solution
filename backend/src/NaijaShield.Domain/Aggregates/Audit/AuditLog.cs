using NaijaShield.Domain.Common;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Domain.Aggregates.Audit;

/// <summary>Append-only, tamper-evident audit log entry.</summary>
public class AuditLog : Entity<Guid>
{
    public Guid? UserId { get; set; }

    /// <summary>User | System | AI</summary>
    public string ActorType { get; set; } = "User";

    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public AuditResult Result { get; set; }
    public AuditSensitivity Sensitivity { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>HMAC-SHA256 chained hash for tamper evidence.</summary>
    public string ChainHash { get; set; } = string.Empty;
}

/// <summary>Data retention policy per category for a tenant.</summary>
public class DataRetentionPolicy : Entity<Guid>
{
    public string DataCategory { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public DateTime LastPurgeRunAt { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>GDPR/NDPR data export (right of access) request.</summary>
public class DataExportRequest : AggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public ExportRequestStatus Status { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string OutputBlobUri { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}

/// <summary>An external integration (Twilio, EFCC API, etc.) registered for a tenant.</summary>
public class Integration : AggregateRoot<Guid>
{
    public IntegrationCategory Category { get; set; }
    public string Provider { get; set; } = string.Empty;

    /// <summary>Azure Key Vault secret path — never store the actual secret here.</summary>
    public string SecretReference { get; set; } = string.Empty;

    public bool IsConnected { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string LastError { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}

/// <summary>Outbox message for reliable, at-least-once event publishing.</summary>
public class OutboxMessage : Entity<Guid>
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>Feature flag with tenant-level overrides and rollout percentage.</summary>
public class FeatureFlag : Entity<Guid>
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool DefaultValue { get; set; }
    public string TenantOverridesJson { get; set; } = "{}";
    public decimal RolloutPercentage { get; set; } = 100;
    public Guid TenantId { get; set; }
}
