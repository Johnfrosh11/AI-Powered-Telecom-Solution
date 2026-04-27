using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Features.Audit;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AuditLogDto(
    Guid Id, string UserId, string UserEmail, string Action, string TargetType,
    string? TargetId, string IpAddress, string UserAgent, string Result,
    string Sensitivity, string ChainHash, DateTime OccurredAt, Guid TenantId);

// ── List Audit Logs ───────────────────────────────────────────────────────────

public record ListAuditLogsQuery(
    Guid TenantId, string? UserId, string? Action, DateTime? From, DateTime? To,
    int Page = 1, int PageSize = 100)
    : IRequest<Result<PagedResult<AuditLogDto>>>;

public class ListAuditLogsQueryHandler(IAuditLogRepository auditLogs)
    : IRequestHandler<ListAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    public async Task<Result<PagedResult<AuditLogDto>>> Handle(ListAuditLogsQuery q, CancellationToken ct)
    {
        var items = await auditLogs.ListAsync(q.TenantId, Guid.TryParse(q.UserId, out var actorId) ? actorId : null, q.Action, q.From, q.To, q.Page, q.PageSize, ct);
        var total = await auditLogs.CountAsync(q.TenantId, ct);

        var dtos = items.Select(a => new AuditLogDto(
            a.Id, a.UserId?.ToString() ?? string.Empty, string.Empty, a.Action, a.TargetType,
            a.TargetId, a.IpAddress, a.UserAgent, a.Result.ToString(),
            a.Sensitivity.ToString(), a.ChainHash, a.OccurredAt, a.TenantId)).ToList();

        return Result.Success(new PagedResult<AuditLogDto>(dtos, total, q.Page, q.PageSize));
    }
}
