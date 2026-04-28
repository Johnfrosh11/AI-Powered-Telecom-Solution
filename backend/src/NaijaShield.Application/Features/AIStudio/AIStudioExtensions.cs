using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.ScamDetection;

namespace NaijaShield.Application.Features.AIStudio;

// ── Get by ID ─────────────────────────────────────────────────────────────────

public record GetPromptTemplateByIdQuery(Guid TemplateId, Guid TenantId)
    : IRequest<Result<PromptTemplateDto>>;

public class GetPromptTemplateByIdQueryHandler(IPromptTemplateRepository prompts)
    : IRequestHandler<GetPromptTemplateByIdQuery, Result<PromptTemplateDto>>
{
    public async Task<Result<PromptTemplateDto>> Handle(GetPromptTemplateByIdQuery q, CancellationToken ct)
    {
        var p = await prompts.GetByIdAsync(q.TemplateId, ct);
        if (p is null || p.TenantId != q.TenantId)
            return Result.Failure<PromptTemplateDto>("Prompt template not found.");

        return Result.Success(new PromptTemplateDto(
            p.Id, p.Name, p.Language, p.UseCase, p.Content,
            p.Version, p.IsActive, p.TimesUsed, p.SuccessRate, p.TenantId));
    }
}

// ── Create Prompt Template ────────────────────────────────────────────────────

public record CreatePromptTemplateCommand(
    Guid TenantId, Guid CreatedByUserId, string Name, string Language, string UseCase, string Content)
    : IRequest<Result<Guid>>, ITransactionalCommand;

public class CreatePromptTemplateCommandHandler(IPromptTemplateRepository prompts)
    : IRequestHandler<CreatePromptTemplateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePromptTemplateCommand cmd, CancellationToken ct)
    {
        var template = PromptTemplate.Create(cmd.TenantId, cmd.CreatedByUserId, cmd.Name, cmd.Language, cmd.UseCase, cmd.Content);
        await prompts.AddAsync(template, ct);
        return Result.Success(template.Id);
    }
}

// ── Train Pattern Command ─────────────────────────────────────────────────────

public record TrainPatternCommand(Guid PatternId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand;

public class TrainPatternCommandHandler(IScamPatternRepository patterns)
    : IRequestHandler<TrainPatternCommand, Result>
{
    public async Task<Result> Handle(TrainPatternCommand cmd, CancellationToken ct)
    {
        var pattern = await patterns.GetByIdAsync(cmd.PatternId, ct);
        if (pattern is null || pattern.TenantId != cmd.TenantId)
            return Result.Failure("Pattern not found.");

        // Trigger accuracy recalculation
        var accuracy = pattern.TimesTriggered > 0 ? Math.Min(0.99m, 0.70m + pattern.TimesTriggered * 0.001m) : 0.70m;
        pattern.UpdateAccuracy(accuracy);
        return Result.Success();
    }
}
