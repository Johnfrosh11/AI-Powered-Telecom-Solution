using NaijaShield.Domain.Common;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Domain.Aggregates.Tenants;

/// <summary>Represents a telco operator tenant (MTN, Airtel, Glo, 9mobile).</summary>
public class Tenant : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string NccOperatorId { get; private set; } = string.Empty;
    public string LogoUrl { get; private set; } = string.Empty;
    public string TimeZone { get; private set; } = "Africa/Lagos";
    public string DefaultLanguage { get; private set; } = "en";
    public TenantStatus Status { get; private set; }
    public SubscriptionPlan Plan { get; private set; }

    // Settings
    public bool MfaRequired { get; private set; }
    public bool SsoEnabled { get; private set; }
    public bool AiAutoBlockEnabled { get; private set; }
    public decimal BlockingConfidenceThreshold { get; private set; } = 0.85m;
    public int MaxApiCallsPerMinute { get; private set; } = 100;

    private Tenant() { }

    public static Tenant Create(
        string name,
        string nccOperatorId,
        string logoUrl,
        SubscriptionPlan plan = SubscriptionPlan.Trial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nccOperatorId);

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" ", "-"),
            NccOperatorId = nccOperatorId,
            LogoUrl = logoUrl,
            Status = TenantStatus.Trial,
            Plan = plan,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateSettings(bool? mfaRequired, bool? ssoEnabled, decimal? threshold, int? maxApiCalls)
    {
        if (mfaRequired.HasValue) MfaRequired = mfaRequired.Value;
        if (ssoEnabled.HasValue) SsoEnabled = ssoEnabled.Value;
        if (threshold.HasValue) BlockingConfidenceThreshold = threshold.Value;
        if (maxApiCalls.HasValue) MaxApiCallsPerMinute = maxApiCalls.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePlan(SubscriptionPlan plan)
    {
        Plan = plan;
        UpdatedAt = DateTime.UtcNow;
    }
}
