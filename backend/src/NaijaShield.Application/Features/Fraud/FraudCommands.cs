using FluentValidation;
using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.ScamDetection;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Application.Features.Fraud;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ScamCallDto(
    Guid Id, string CallerMsisdn, string ReceiverMsisdn, DateTime StartedAt,
    TimeSpan Duration, string DetectedLanguage, decimal AiConfidenceScore,
    string Status, bool WarningSmsSent, int VictimsWarned,
    decimal? EstimatedMoneySaved, string AiReasoning, Guid TenantId);

public record ScamPatternDto(
    Guid Id, string Name, string Description, string Category,
    string Severity, bool IsActive, decimal DetectionAccuracy,
    int TimesTriggered, Guid TenantId);

public record WatchlistedNumberDto(
    Guid Id, string MaskedNumber, string DetectedLanguages,
    int TotalCallsMade, int VictimsReached, DateTime FirstSeen,
    DateTime LastSeen, string Status, Guid TenantId);

// ── Ingest Call Audio (kicks off AI pipeline) ─────────────────────────────────

public record IngestCallAudioCommand(
    Guid TenantId,
    string CallerMsisdn,
    string ReceiverMsisdn,
    DateTime StartedAt,
    TimeSpan Duration,
    Stream AudioStream,
    string SuspectedLanguage = "en")
    : IRequest<Result<Guid>>, ITransactionalCommand;

public class IngestCallAudioCommandHandler(
    IScamDetectionAiService aiService,
    IScamCallRepository scamCalls,
    IScamPatternRepository patterns,
    IRealtimeNotifier realtime,
    ISmsGateway smsGateway)
    : IRequestHandler<IngestCallAudioCommand, Result<Guid>>
{
    private const decimal ConfidenceThreshold = 0.85m;

    public async Task<Result<Guid>> Handle(IngestCallAudioCommand cmd, CancellationToken ct)
    {
        // Step 1: Transcribe
        var transcript = await aiService.TranscribeAudioAsync(cmd.AudioStream, cmd.SuspectedLanguage, ct);

        // Step 2: Detect language
        var language = await aiService.DetectLanguageAsync(transcript, ct);

        // Step 3: Translate if not English
        var transcriptEn = language == "en"
            ? transcript
            : await aiService.TranslateAsync(transcript, language, "en", ct);

        // Step 4: Classify scam
        var classification = await aiService.ClassifyScamAsync(transcriptEn, language, ct);

        // Step 5: Extract entities
        var entities = await aiService.ExtractEntitiesAsync(transcriptEn, ct);

        var scamCall = ScamCall.CreateDetected(
            cmd.TenantId,
            cmd.CallerMsisdn,
            cmd.ReceiverMsisdn,
            cmd.StartedAt,
            cmd.Duration,
            language,
            audioBlobUri: string.Empty, // set by blob upload caller
            transcriptOriginal: transcript,
            transcriptEnglish: transcriptEn,
            aiConfidenceScore: classification.Confidence,
            matchedPatternId: classification.MatchedPatternId,
            aiReasoning: classification.Reasoning);

        await scamCalls.AddAsync(scamCall, ct);

        // Step 6: Warn victim if confidence is high
        if (classification.Confidence >= ConfidenceThreshold)
        {
            var smsBody = await aiService.GenerateWarningSmsAsync(language, classification, ct);
            await smsGateway.SendAsync(cmd.ReceiverMsisdn, smsBody, ct);
            scamCall.MarkWarningSent(victimsWarned: 1);

            // Update matched pattern counter
            if (classification.MatchedPatternId.HasValue)
            {
                var pattern = await patterns.GetByIdAsync(classification.MatchedPatternId.Value, ct);
                pattern?.IncrementTriggerCount();
            }
        }

        // Step 7: Push real-time alert
        await realtime.NotifyTenantAsync(
            cmd.TenantId,
            "scam.detected",
            new { scamCall.Id, classification.Confidence, language, cmd.CallerMsisdn },
            ct);

        return Result.Success(scamCall.Id);
    }
}

// ── Confirm Scam Call ─────────────────────────────────────────────────────────

public record ConfirmScamCallCommand(Guid ScamCallId, Guid TenantId, Guid ConfirmedByUserId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "fraud.calls.confirm";
    public string AuditTargetType => "ScamCall";
    public string? AuditTargetId => ScamCallId.ToString();
}

public class ConfirmScamCallCommandHandler(IScamCallRepository scamCalls)
    : IRequestHandler<ConfirmScamCallCommand, Result>
{
    public async Task<Result> Handle(ConfirmScamCallCommand cmd, CancellationToken ct)
    {
        var call = await scamCalls.GetByIdAsync(cmd.ScamCallId, ct);
        if (call is null || call.TenantId != cmd.TenantId)
            return Result.Failure("Scam call not found.");

        if (call.Status != ScamCallStatus.Detected)
            return Result.Failure("Only detected calls can be confirmed.");

        call.Confirm(cmd.ConfirmedByUserId);
        return Result.Success();
    }
}

// ── Mark False Positive ───────────────────────────────────────────────────────

public record MarkFalsePositiveCommand(Guid ScamCallId, Guid TenantId, Guid ReviewedByUserId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "fraud.calls.mark_false_positive";
    public string AuditTargetType => "ScamCall";
    public string? AuditTargetId => ScamCallId.ToString();
}

public class MarkFalsePositiveCommandHandler(IScamCallRepository scamCalls)
    : IRequestHandler<MarkFalsePositiveCommand, Result>
{
    public async Task<Result> Handle(MarkFalsePositiveCommand cmd, CancellationToken ct)
    {
        var call = await scamCalls.GetByIdAsync(cmd.ScamCallId, ct);
        if (call is null || call.TenantId != cmd.TenantId)
            return Result.Failure("Scam call not found.");

        call.MarkFalsePositive(cmd.ReviewedByUserId);
        return Result.Success();
    }
}

// ── Send Warning ──────────────────────────────────────────────────────────────

public record SendScamWarningCommand(Guid ScamCallId, Guid TenantId, WarningChannel Channel)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "fraud.warnings.send";
    public string AuditTargetType => "ScamCall";
    public string? AuditTargetId => ScamCallId.ToString();
}

public class SendScamWarningCommandHandler(
    IScamCallRepository scamCalls,
    IScamDetectionAiService aiService,
    ISmsGateway smsGateway,
    IWhatsAppGateway whatsAppGateway)
    : IRequestHandler<SendScamWarningCommand, Result>
{
    public async Task<Result> Handle(SendScamWarningCommand cmd, CancellationToken ct)
    {
        var call = await scamCalls.GetByIdAsync(cmd.ScamCallId, ct);
        if (call is null || call.TenantId != cmd.TenantId)
            return Result.Failure("Scam call not found.");

        var classification = new ScamClassification(
            call.MatchedPatternId, call.AiConfidenceScore, call.AiReasoning, []);

        var message = await aiService.GenerateWarningSmsAsync(call.DetectedLanguage, classification, ct);

        bool sent = cmd.Channel switch
        {
            WarningChannel.Sms => await smsGateway.SendAsync(call.ReceiverMsisdn, message, ct),
            WarningChannel.WhatsApp => await whatsAppGateway.SendAsync(call.ReceiverMsisdn, message, ct),
            _ => false
        };

        if (!sent)
            return Result.Failure("Failed to dispatch warning message.");

        call.MarkWarningSent(victimsWarned: 1);
        return Result.Success();
    }
}

// ── Block Number ──────────────────────────────────────────────────────────────

public record BlockNumberCommand(string Msisdn, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "fraud.watchlist.block";
    public string AuditTargetType => "WatchlistedNumber";
    public string? AuditTargetId => Msisdn;
}

public class BlockNumberCommandHandler(IWatchlistedNumberRepository watchlist)
    : IRequestHandler<BlockNumberCommand, Result>
{
    public async Task<Result> Handle(BlockNumberCommand cmd, CancellationToken ct)
    {
        var entry = await watchlist.GetByMsisdnAsync(cmd.Msisdn, cmd.TenantId, ct);
        if (entry is null)
            return Result.Failure("Number not found in watchlist.");

        entry.Block();
        return Result.Success();
    }
}

// ── Search Scam Calls Query ───────────────────────────────────────────────────

public record SearchScamCallsQuery(
    Guid TenantId, string? Status, string? Language,
    DateTime? From, DateTime? To, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<ScamCallDto>>>;

public class SearchScamCallsQueryHandler(IScamCallRepository scamCalls)
    : IRequestHandler<SearchScamCallsQuery, Result<PagedResult<ScamCallDto>>>
{
    public async Task<Result<PagedResult<ScamCallDto>>> Handle(SearchScamCallsQuery q, CancellationToken ct)
    {
        var items = await scamCalls.SearchAsync(q.TenantId, q.Status, q.Language, q.From, q.To, q.Page, q.PageSize, ct);
        var total = await scamCalls.CountAsync(q.TenantId, q.Status, q.Language, q.From, q.To, ct);

        var dtos = items.Select(c => new ScamCallDto(
            c.Id, c.CallerMsisdn, c.ReceiverMsisdn, c.StartedAt, c.Duration,
            c.DetectedLanguage, c.AiConfidenceScore, c.Status.ToString(),
            c.WarningSmsSent, c.VictimsWarned, c.EstimatedMoneySaved,
            c.AiReasoning, c.TenantId)).ToList();

        return Result.Success(new PagedResult<ScamCallDto>(dtos, total, q.Page, q.PageSize));
    }
}

// ── Get Scam Call By ID ───────────────────────────────────────────────────────

public record GetScamCallByIdQuery(Guid ScamCallId, Guid TenantId)
    : IRequest<Result<ScamCallDto>>;

public class GetScamCallByIdQueryHandler(IScamCallRepository scamCalls)
    : IRequestHandler<GetScamCallByIdQuery, Result<ScamCallDto>>
{
    public async Task<Result<ScamCallDto>> Handle(GetScamCallByIdQuery q, CancellationToken ct)
    {
        var c = await scamCalls.GetByIdAsync(q.ScamCallId, ct);
        if (c is null || c.TenantId != q.TenantId)
            return Result.Failure<ScamCallDto>("Scam call not found.");

        return Result.Success(new ScamCallDto(
            c.Id, c.CallerMsisdn, c.ReceiverMsisdn, c.StartedAt, c.Duration,
            c.DetectedLanguage, c.AiConfidenceScore, c.Status.ToString(),
            c.WarningSmsSent, c.VictimsWarned, c.EstimatedMoneySaved,
            c.AiReasoning, c.TenantId));
    }
}

// ── Create Scam Pattern ───────────────────────────────────────────────────────

public record CreatePatternCommand(
    Guid TenantId, string Name, string Description, string Category,
    string Severity, IReadOnlyList<PhraseInput> Phrases)
    : IRequest<Result<Guid>>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "fraud.patterns.manage";
    public string AuditTargetType => "ScamPattern";
    public string? AuditTargetId => null;
}

public record PhraseInput(string Language, string Phrase, decimal Weight);

public class CreatePatternCommandValidator : AbstractValidator<CreatePatternCommand>
{
    public CreatePatternCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Severity).NotEmpty()
            .Must(s => Enum.TryParse<ScamSeverity>(s, out _))
            .WithMessage("Severity must be: Low, Medium, High, or Critical");
    }
}

public class CreatePatternCommandHandler(IScamPatternRepository patterns)
    : IRequestHandler<CreatePatternCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePatternCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<ScamSeverity>(cmd.Severity, out var severity))
            return Result.Failure<Guid>("Invalid severity.");

        var pattern = ScamPattern.Create(cmd.TenantId, cmd.Name, cmd.Description, cmd.Category, severity);

        foreach (var p in cmd.Phrases)
        {
            pattern.AddPhrase(p.Language, p.Phrase, p.Weight);
        }

        await patterns.AddAsync(pattern, ct);
        return Result.Success(pattern.Id);
    }
}

// ── Get Watchlist Query ───────────────────────────────────────────────────────

public record GetWatchlistQuery(Guid TenantId, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<WatchlistedNumberDto>>>;

public class GetWatchlistQueryHandler(IWatchlistedNumberRepository watchlist)
    : IRequestHandler<GetWatchlistQuery, Result<PagedResult<WatchlistedNumberDto>>>
{
    public async Task<Result<PagedResult<WatchlistedNumberDto>>> Handle(GetWatchlistQuery q, CancellationToken ct)
    {
        var items = await watchlist.ListAsync(q.TenantId, q.Page, q.PageSize, ct);
        var total = await watchlist.CountAsync(q.TenantId, ct);

        var dtos = items.Select(n => new WatchlistedNumberDto(
            n.Id, n.MaskedNumber, n.DetectedLanguages,
            n.TotalCallsMade, n.VictimsReached, n.FirstSeen, n.LastSeen,
            n.Status.ToString(), n.TenantId)).ToList();

        return Result.Success(new PagedResult<WatchlistedNumberDto>(dtos, total, q.Page, q.PageSize));
    }
}
