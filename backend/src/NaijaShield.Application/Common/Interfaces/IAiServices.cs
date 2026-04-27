namespace NaijaShield.Application.Common.Interfaces;

/// <summary>Result of scam classification by the AI pipeline.</summary>
public record ScamClassification(
    Guid? MatchedPatternId,
    decimal Confidence,
    string Reasoning,
    IReadOnlyList<string> TriggerPhrases);

/// <summary>Entities extracted from a scam transcript.</summary>
public record ScamEntities(
    IReadOnlyList<string> PhoneNumbers,
    IReadOnlyList<string> BankAccounts,
    IReadOnlyList<string> PersonNames,
    IReadOnlyList<string> OrganizationNames,
    IReadOnlyList<string> Amounts);

/// <summary>Orchestrates the full scam detection AI pipeline.</summary>
public interface IScamDetectionAiService
{
    Task<string> TranscribeAudioAsync(Stream audio, string suspectedLanguage, CancellationToken ct = default);
    Task<string> DetectLanguageAsync(string text, CancellationToken ct = default);
    Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken ct = default);
    Task<ScamClassification> ClassifyScamAsync(string transcript, string language, CancellationToken ct = default);
    Task<ScamEntities> ExtractEntitiesAsync(string transcript, CancellationToken ct = default);
    Task<string> GenerateWarningSmsAsync(string language, ScamClassification classification, CancellationToken ct = default);
    Task<string> GenerateAiSuggestedReplyAsync(string conversationContext, string language, CancellationToken ct = default);
    Task<string> SummarizeConversationAsync(string conversationContext, CancellationToken ct = default);
    Task<string> GenerateExecutiveBriefAsync(DateTime from, DateTime to, Guid tenantId, CancellationToken ct = default);
}
