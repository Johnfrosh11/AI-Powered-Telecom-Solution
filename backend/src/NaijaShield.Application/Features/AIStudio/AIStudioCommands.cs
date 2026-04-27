using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Features.AIStudio;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record PromptTemplateDto(
    Guid Id, string Name, string Language, string UseCase, string Content,
    int Version, bool IsActive, int TimesUsed, decimal SuccessRate, Guid TenantId);

public record SandboxTestRequest(string InputText, string Language, string? ModelOverride);

public record SandboxTestResult(
    string Classification,
    decimal Confidence,
    string Reasoning,
    string SuggestedResponse,
    IReadOnlyList<string> TriggerPhrases,
    int TokensUsed);

// ── Sandbox Test ──────────────────────────────────────────────────────────────

public record SandboxTestCommand(
    Guid TenantId, string InputText, string Language, string? ModelOverride)
    : IRequest<Result<SandboxTestResult>>;

public class SandboxTestCommandHandler(IScamDetectionAiService aiService)
    : IRequestHandler<SandboxTestCommand, Result<SandboxTestResult>>
{
    public async Task<Result<SandboxTestResult>> Handle(SandboxTestCommand cmd, CancellationToken ct)
    {
        var textEn = cmd.Language == "en"
            ? cmd.InputText
            : await aiService.TranslateAsync(cmd.InputText, cmd.Language, "en", ct);

        var classification = await aiService.ClassifyScamAsync(textEn, cmd.Language, ct);
        var reply = await aiService.GenerateAiSuggestedReplyAsync(cmd.InputText, cmd.Language, ct);

        return Result.Success(new SandboxTestResult(
            Classification: classification.MatchedPatternId.HasValue ? "ScamDetected" : "Clean",
            Confidence: classification.Confidence,
            Reasoning: classification.Reasoning,
            SuggestedResponse: reply,
            TriggerPhrases: classification.TriggerPhrases,
            TokensUsed: 0 // populated by infrastructure layer
        ));
    }
}

// ── List Prompt Templates ─────────────────────────────────────────────────────

public record ListPromptsQuery(Guid TenantId, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<PromptTemplateDto>>>;

public class ListPromptsQueryHandler(IPromptTemplateRepository prompts)
    : IRequestHandler<ListPromptsQuery, Result<PagedResult<PromptTemplateDto>>>
{
    public async Task<Result<PagedResult<PromptTemplateDto>>> Handle(ListPromptsQuery q, CancellationToken ct)
    {
        var items = await prompts.ListAsync(q.TenantId, q.Page, q.PageSize, ct);
        var dtos = items.Select(p => new PromptTemplateDto(
            p.Id, p.Name, p.Language, p.UseCase, p.Content,
            p.Version, p.IsActive, p.TimesUsed, p.SuccessRate, p.TenantId)).ToList();

        return Result.Success(new PagedResult<PromptTemplateDto>(dtos, dtos.Count, q.Page, q.PageSize));
    }
}
