using NaijaShield.Domain.Common;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Domain.Aggregates.Customers;

/// <summary>Telco subscriber (potential scam victim or complainant).</summary>
public class Customer : AggregateRoot<Guid>
{
    /// <summary>SHA-256 hash of the E.164 phone number for privacy-preserving lookup.</summary>
    public string HashedMsisdn { get; private set; } = string.Empty;

    /// <summary>Masked for display, e.g. +234801***5678.</summary>
    public string MaskedMsisdn { get; private set; } = string.Empty;

    /// <summary>Full name — partially masked for display.</summary>
    public string FullNameMasked { get; private set; } = string.Empty;

    /// <summary>SHA-256 hash of NIN, never stored in plaintext.</summary>
    public string NinHashed { get; private set; } = string.Empty;

    public string PreferredLanguage { get; private set; } = "en";
    public string Plan { get; private set; } = string.Empty;
    public string Region { get; private set; } = string.Empty;
    public DateTime ActivatedAt { get; private set; }
    public DateTime FirstSeen { get; private set; }
    public DateTime? LastContactedAt { get; private set; }
    public CustomerStatus Status { get; private set; }

    /// <summary>Lifetime value in Kobo (smallest NGN unit).</summary>
    public decimal LifetimeValue { get; private set; }
    public decimal LifetimeValueKobo { get; private set; }
    public decimal FraudRiskScore { get; private set; }

    public int TotalInteractions { get; private set; }
    public int FraudInteractions { get; private set; }

    public bool NinSimLinked { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<Interaction> _interactions = [];
    public IReadOnlyCollection<Interaction> Interactions => _interactions.AsReadOnly();

    private Customer() { }

    public static Customer Create(
        Guid tenantId,
        string msisdn,
        string fullNameMasked,
        string preferredLanguage = "en",
        string plan = "Prepaid")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(msisdn);

        var masked = msisdn.Length > 8 ? msisdn[..7] + "***" + msisdn[^4..] : msisdn;
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(msisdn)));

        return new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HashedMsisdn = hash,
            MaskedMsisdn = masked,
            FullNameMasked = fullNameMasked,
            PreferredLanguage = preferredLanguage,
            Plan = plan,
            Status = CustomerStatus.Active,
            ActivatedAt = DateTime.UtcNow,
            FirstSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Bar() { Status = CustomerStatus.Barred; UpdatedAt = DateTime.UtcNow; }
    public void Suspend() { Status = CustomerStatus.Suspended; UpdatedAt = DateTime.UtcNow; }
    public void MarkChurned() { Status = CustomerStatus.Churned; UpdatedAt = DateTime.UtcNow; }

    public void LinkNin(string ninHash)
    {
        NinHashed = ninHash;
        NinSimLinked = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>A single interaction (call, SMS, WhatsApp session) with a customer.</summary>
public class Interaction : Entity<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid TenantId { get; set; }
    public Channel Channel { get; set; }
    public string Direction { get; set; } = "Inbound"; // Inbound | Outbound
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string DetectedLanguage { get; set; } = "en";
    public decimal SentimentScore { get; set; }
    public InteractionStatus Status { get; set; }
    public Guid? AiAgentId { get; set; }
    public Guid? HumanAgentId { get; set; }
}
