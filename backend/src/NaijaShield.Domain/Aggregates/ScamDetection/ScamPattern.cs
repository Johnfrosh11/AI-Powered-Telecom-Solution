using NaijaShield.Domain.Common;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Domain.Aggregates.ScamDetection;

/// <summary>A reusable scam detection pattern with per-language trigger phrases.</summary>
public class ScamPattern : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public ScamSeverity Severity { get; private set; }
    public bool IsActive { get; private set; }
    public decimal DetectionAccuracy { get; private set; }
    public int TimesTriggered { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<ScamPatternPhrase> _phrases = [];
    public IReadOnlyCollection<ScamPatternPhrase> Phrases => _phrases.AsReadOnly();

    private ScamPattern() { }

    public static ScamPattern Create(
        Guid tenantId,
        string name,
        string description,
        string category,
        ScamSeverity severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ScamPattern
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            Category = category,
            Severity = severity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddPhrase(string language, string phrase, decimal weight)
    {
        _phrases.Add(ScamPatternPhrase.Create(Id, language, phrase, weight));
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementTriggerCount()
    {
        TimesTriggered++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAccuracy(decimal accuracy)
    {
        DetectionAccuracy = accuracy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
}

/// <summary>A per-language trigger phrase for a scam pattern.</summary>
public class ScamPatternPhrase : Entity<Guid>
{
    public Guid PatternId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Phrase { get; set; } = string.Empty;

    /// <summary>Contribution weight to the overall confidence score (0–1).</summary>
    public decimal Weight { get; set; }

    internal static ScamPatternPhrase Create(Guid patternId, string language, string phrase, decimal weight) =>
        new() { Id = Guid.NewGuid(), PatternId = patternId, Language = language, Phrase = phrase, Weight = weight };
}

/// <summary>A phone number flagged as a scam originator.</summary>
public class WatchlistedNumber : AggregateRoot<Guid>
{
    /// <summary>SHA-256 hash of the E.164 phone number for privacy-preserving lookup.</summary>
    public string HashedMsisdn { get; private set; } = string.Empty;

    /// <summary>Masked for display, e.g. +234801***5678.</summary>
    public string MaskedNumber { get; private set; } = string.Empty;

    /// <summary>Comma-separated language codes detected.</summary>
    public string DetectedLanguages { get; private set; } = string.Empty;

    public Guid PrimaryPatternId { get; private set; }
    public int TotalCallsMade { get; private set; }
    public int VictimsReached { get; private set; }
    public DateTime FirstSeen { get; private set; }
    public DateTime LastSeen { get; private set; }
    public WatchlistStatus Status { get; private set; }
    public Guid TenantId { get; private set; }

    private WatchlistedNumber() { }

    public static WatchlistedNumber Create(
        Guid tenantId,
        string msisdn,
        Guid primaryPatternId,
        string detectedLanguages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(msisdn);

        return new WatchlistedNumber
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HashedMsisdn = HashMsisdn(msisdn),
            MaskedNumber = MaskNumber(msisdn),
            PrimaryPatternId = primaryPatternId,
            DetectedLanguages = detectedLanguages,
            Status = WatchlistStatus.Monitored,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void RecordCall(int victims = 0)
    {
        TotalCallsMade++;
        VictimsReached += victims;
        LastSeen = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Block() { Status = WatchlistStatus.Blocked; UpdatedAt = DateTime.UtcNow; }
    public void ReportToEfcc() { Status = WatchlistStatus.ReportedToEFCC; UpdatedAt = DateTime.UtcNow; }
    public void Whitelist() { Status = WatchlistStatus.Whitelisted; UpdatedAt = DateTime.UtcNow; }

    private static string MaskNumber(string msisdn)
    {
        if (msisdn.Length < 8) return msisdn;
        return msisdn[..7] + "***" + msisdn[^4..];
    }

    private static string HashMsisdn(string msisdn)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(msisdn));
        return Convert.ToBase64String(bytes);
    }
}

/// <summary>A warning message sent to a potential scam victim.</summary>
public class ScamWarning : AggregateRoot<Guid>
{
    public Guid ScamCallId { get; private set; }
    public string RecipientMsisdn { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public string MessageBody { get; private set; } = string.Empty;
    public WarningChannel Channel { get; private set; }
    public WarningStatus Status { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public Guid TenantId { get; private set; }

    private ScamWarning() { }

    public static ScamWarning Create(
        Guid tenantId,
        Guid scamCallId,
        string recipientMsisdn,
        string language,
        string messageBody,
        WarningChannel channel)
    {
        return new ScamWarning
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScamCallId = scamCallId,
            RecipientMsisdn = recipientMsisdn,
            Language = language,
            MessageBody = messageBody,
            Channel = channel,
            Status = WarningStatus.Pending,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkSent() { Status = WarningStatus.Sent; UpdatedAt = DateTime.UtcNow; }
    public void MarkDelivered() { Status = WarningStatus.Delivered; DeliveredAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void MarkFailed() { Status = WarningStatus.Failed; UpdatedAt = DateTime.UtcNow; }
}
