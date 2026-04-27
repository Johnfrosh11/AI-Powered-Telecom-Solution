using NaijaShield.Domain.Aggregates.ScamDetection.Events;
using NaijaShield.Domain.Common;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Domain.Aggregates.ScamDetection;

/// <summary>Core aggregate — a detected or confirmed scam phone call.</summary>
public class ScamCall : AggregateRoot<Guid>
{
    public string CallerMsisdn { get; private set; } = string.Empty;
    public string ReceiverMsisdn { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string DetectedLanguage { get; private set; } = "en";
    public string AudioBlobUri { get; private set; } = string.Empty;
    public string TranscriptOriginal { get; private set; } = string.Empty;
    public string TranscriptEnglish { get; private set; } = string.Empty;
    public decimal AiConfidenceScore { get; private set; }
    public Guid? MatchedPatternId { get; private set; }
    public ScamCallStatus Status { get; private set; }
    public bool WarningSmsSent { get; private set; }
    public int VictimsWarned { get; private set; }

    /// <summary>Estimated money saved in Kobo.</summary>
    public decimal? EstimatedMoneySaved { get; private set; }

    public Guid TenantId { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>Full chain-of-thought reasoning from Semantic Kernel.</summary>
    public string AiReasoning { get; private set; } = string.Empty;

    private ScamCall() { }

    public static ScamCall CreateDetected(
        Guid tenantId,
        string callerMsisdn,
        string receiverMsisdn,
        DateTime startedAt,
        TimeSpan duration,
        string detectedLanguage,
        string audioBlobUri,
        string transcriptOriginal,
        string transcriptEnglish,
        decimal aiConfidenceScore,
        Guid? matchedPatternId,
        string aiReasoning)
    {
        var call = new ScamCall
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CallerMsisdn = callerMsisdn,
            ReceiverMsisdn = receiverMsisdn,
            StartedAt = startedAt,
            Duration = duration,
            DetectedLanguage = detectedLanguage,
            AudioBlobUri = audioBlobUri,
            TranscriptOriginal = transcriptOriginal,
            TranscriptEnglish = transcriptEnglish,
            AiConfidenceScore = aiConfidenceScore,
            MatchedPatternId = matchedPatternId,
            AiReasoning = aiReasoning,
            Status = ScamCallStatus.Detected,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        call.RaiseDomainEvent(new ScamCallDetectedEvent(call.Id, tenantId, callerMsisdn, receiverMsisdn, aiConfidenceScore, detectedLanguage));
        return call;
    }

    public void Confirm(Guid confirmedByUserId)
    {
        Status = ScamCallStatus.Confirmed;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ScamCallConfirmedEvent(Id, TenantId, confirmedByUserId));
    }

    public void MarkFalsePositive(Guid reviewedByUserId)
    {
        Status = ScamCallStatus.FalsePositive;
        ConfirmedByUserId = reviewedByUserId;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReported()
    {
        Status = ScamCallStatus.Reported;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkWarningSent(int victimsWarned)
    {
        WarningSmsSent = true;
        VictimsWarned = victimsWarned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEstimatedSavings(decimal amountInKobo)
    {
        EstimatedMoneySaved = amountInKobo;
        UpdatedAt = DateTime.UtcNow;
    }
}
