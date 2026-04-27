using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Tenants;

namespace NaijaShield.Application.Features.Settings;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record TenantSettingsDto(
    Guid Id, string Name, string Slug, string Plan, string Status,
    bool MfaRequired, bool SsoEnabled, bool AiAutoBlockEnabled,
    decimal BlockingConfidenceThreshold, int MaxApiCallsPerMinute);

// ── Get Tenant Settings ───────────────────────────────────────────────────────

public record GetTenantSettingsQuery(Guid TenantId)
    : IRequest<Result<TenantSettingsDto>>;

public class GetTenantSettingsQueryHandler(ITenantRepository tenants)
    : IRequestHandler<GetTenantSettingsQuery, Result<TenantSettingsDto>>
{
    public async Task<Result<TenantSettingsDto>> Handle(GetTenantSettingsQuery q, CancellationToken ct)
    {
        var t = await tenants.GetByIdAsync(q.TenantId, ct);
        if (t is null)
            return Result.Failure<TenantSettingsDto>("Tenant not found.");

        return Result.Success(new TenantSettingsDto(
            t.Id, t.Name, t.Slug, t.Plan.ToString(), t.Status.ToString(),
            t.MfaRequired, t.SsoEnabled, t.AiAutoBlockEnabled,
            t.BlockingConfidenceThreshold, t.MaxApiCallsPerMinute));
    }
}

// ── Update Tenant Settings ────────────────────────────────────────────────────

public record UpdateTenantSettingsCommand(
    Guid TenantId,
    bool? MfaRequired,
    bool? AiAutoBlockEnabled,
    decimal? BlockingConfidenceThreshold,
    int? MaxApiCallsPerMinute)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "settings.update";
    public string AuditTargetType => "Tenant";
    public string? AuditTargetId => TenantId.ToString();
    public string AuditSensitivity => "High";
}

public class UpdateTenantSettingsCommandHandler(ITenantRepository tenants)
    : IRequestHandler<UpdateTenantSettingsCommand, Result>
{
    public async Task<Result> Handle(UpdateTenantSettingsCommand cmd, CancellationToken ct)
    {
        var tenant = await tenants.GetByIdAsync(cmd.TenantId, ct);
        if (tenant is null)
            return Result.Failure("Tenant not found.");

        tenant.UpdateSettings(
            cmd.MfaRequired,
            cmd.AiAutoBlockEnabled,
            cmd.BlockingConfidenceThreshold,
            cmd.MaxApiCallsPerMinute);

        return Result.Success();
    }
}
