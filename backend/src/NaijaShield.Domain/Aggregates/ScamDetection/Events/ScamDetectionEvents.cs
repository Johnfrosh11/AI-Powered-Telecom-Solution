using MediatR;

namespace NaijaShield.Domain.Aggregates.ScamDetection.Events;

/// <summary>Raised when AI detects a potential scam call.</summary>
public sealed record ScamCallDetectedEvent(
    Guid ScamCallId,
    Guid TenantId,
    string CallerMsisdn,
    string ReceiverMsisdn,
    decimal AiConfidenceScore,
    string DetectedLanguage) : INotification;

/// <summary>Raised when a fraud analyst confirms a scam call.</summary>
public sealed record ScamCallConfirmedEvent(
    Guid ScamCallId,
    Guid TenantId,
    Guid ConfirmedByUserId) : INotification;

/// <summary>Raised when a scam warning SMS/WhatsApp is dispatched.</summary>
public sealed record ScamWarningSentEvent(
    Guid ScamCallId,
    Guid TenantId,
    string RecipientMsisdn,
    string Language,
    string Channel) : INotification;

/// <summary>Raised when a number is added to the watchlist.</summary>
public sealed record NumberWatchlistedEvent(
    Guid WatchlistedNumberId,
    Guid TenantId,
    string Msisdn) : INotification;
