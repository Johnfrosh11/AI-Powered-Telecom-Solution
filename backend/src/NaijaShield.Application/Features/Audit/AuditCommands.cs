using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Audit;

namespace NaijaShield.Application.Features.Audit;

// ── Get by ID ─────────────────────────────────────────────────────────────────

public record GetAuditLogByIdQuery(Guid AuditLogId, Guid TenantId)
    : IRequest<Result<AuditLogDto>>;

public class GetAuditLogByIdQueryHandler(IAuditLogRepository auditLogs)
    : IRequestHandler<GetAuditLogByIdQuery, Result<AuditLogDto>>
{
    public async Task<Result<AuditLogDto>> Handle(GetAuditLogByIdQuery q, CancellationToken ct)
    {
        var a = await auditLogs.GetByIdAsync(q.AuditLogId, ct);
        if (a is null || a.TenantId != q.TenantId)
            return Result.Failure<AuditLogDto>("Audit log entry not found.");

        return Result.Success(new AuditLogDto(
            a.Id, a.UserId?.ToString() ?? string.Empty, string.Empty, a.Action, a.TargetType,
            a.TargetId, a.IpAddress, a.UserAgent, a.Result.ToString(),
            a.Sensitivity.ToString(), a.ChainHash, a.OccurredAt, a.TenantId));
    }
}

// ── Export Command ────────────────────────────────────────────────────────────

public record ExportAuditLogsCommand(
    Guid TenantId, DateTime From, DateTime To, Guid RequestedByUserId)
    : IRequest<Result<string>>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "audit.export";
    public string AuditTargetType => "AuditLog";
    public string? AuditTargetId => null;
    public string AuditSensitivity => "High";
}

public class ExportAuditLogsCommandHandler(
    IAuditLogRepository auditLogs,
    IAzureBlobStorage blobStorage)
    : IRequestHandler<ExportAuditLogsCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ExportAuditLogsCommand cmd, CancellationToken ct)
    {
        var logs = await auditLogs.ListAsync(cmd.TenantId, null, null, cmd.From, cmd.To, 1, 10_000, ct);
        var csv = "Id,UserId,Action,TargetType,TargetId,IpAddress,Result,Sensitivity,OccurredAt\n" +
            string.Join("\n", logs.Select(a =>
                $"{a.Id},{a.UserId},{a.Action},{a.TargetType},{a.TargetId},{a.IpAddress},{a.Result},{a.Sensitivity},{a.OccurredAt:O}"));

        var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var blobName = $"audit-exports/{cmd.TenantId}/{cmd.RequestedByUserId}/{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        var uri = await blobStorage.UploadAsync(stream, "audit-exports", blobName, "text/csv", ct);
        return Result.Success(uri);
    }
}
