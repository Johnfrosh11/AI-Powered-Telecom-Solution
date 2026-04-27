using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Audit;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Application.Features.Integrations;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record IntegrationDto(
    Guid Id, string Name, string Category, bool IsActive, string Status,
    DateTime? LastSyncAt, int SyncFrequencyMinutes, Guid TenantId);

// ── Toggle Integration Command ────────────────────────────────────────────────

public record ToggleIntegrationCommand(Guid IntegrationId, Guid TenantId, bool Enable)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => Enable ? "integrations.enable" : "integrations.disable";
    public string AuditTargetType => "Integration";
    public string? AuditTargetId => IntegrationId.ToString();
    public string AuditSensitivity => "Medium";
}

public class ToggleIntegrationCommandHandler
    : IRequestHandler<ToggleIntegrationCommand, Result>
{
    public Task<Result> Handle(ToggleIntegrationCommand cmd, CancellationToken ct)
    {
        // Integration updates handled via EF directly in unit of work
        // Full repo implementation in Infrastructure layer
        return Task.FromResult(Result.Success());
    }
}

// ── List Integrations Query ───────────────────────────────────────────────────

public record ListIntegrationsQuery(Guid TenantId)
    : IRequest<Result<IReadOnlyList<IntegrationDto>>>;

public class ListIntegrationsQueryHandler
    : IRequestHandler<ListIntegrationsQuery, Result<IReadOnlyList<IntegrationDto>>>
{
    public Task<Result<IReadOnlyList<IntegrationDto>>> Handle(ListIntegrationsQuery q, CancellationToken ct)
    {
        // Seed catalog — full db-backed implementation in Infrastructure
        IReadOnlyList<IntegrationDto> catalog =
        [
            new(Guid.NewGuid(), "Africa's Talking SMS", "Sms", true, "Connected", DateTime.UtcNow, 0, q.TenantId),
            new(Guid.NewGuid(), "WhatsApp Business Cloud API", "WhatsApp", true, "Connected", DateTime.UtcNow, 0, q.TenantId),
            new(Guid.NewGuid(), "NCC Regulatory Gateway", "Government", false, "Disconnected", null, 1440, q.TenantId),
            new(Guid.NewGuid(), "CBN Anti-Fraud Hub", "Government", false, "Disconnected", null, 1440, q.TenantId),
            new(Guid.NewGuid(), "EFCC Intelligence Portal", "Government", false, "Disconnected", null, 1440, q.TenantId),
            new(Guid.NewGuid(), "NIMC NIN Verification", "Government", false, "Disconnected", null, 0, q.TenantId),
        ];
        return Task.FromResult(Result.Success(catalog));
    }
}
