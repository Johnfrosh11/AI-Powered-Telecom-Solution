using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Reports;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Application.Features.Reports;

// ── Get Report By ID Query ────────────────────────────────────────────────────

public record GetReportByIdQuery(Guid ReportId, Guid TenantId)
    : IRequest<Result<ReportDto>>;

public class GetReportByIdQueryHandler(IRegulatoryReportRepository reports)
    : IRequestHandler<GetReportByIdQuery, Result<ReportDto>>
{
    public async Task<Result<ReportDto>> Handle(GetReportByIdQuery q, CancellationToken ct)
    {
        var r = await reports.GetByIdAsync(q.ReportId, ct);
        if (r is null || r.TenantId != q.TenantId)
            return Result.Failure<ReportDto>("Report not found.");

        return Result.Success(new ReportDto(
            r.Id, r.Type.ToString(), r.PeriodStart, r.PeriodEnd,
            r.Status.ToString(), r.GeneratedAt, r.OutputBlobUri,
            r.SubmittedAt, r.RegulatorReference, r.TenantId));
    }
}

// ── Submit to EFCC Command ────────────────────────────────────────────────────

public record SubmitReportToEfccCommand(Guid ReportId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "report.submitted_efcc";
    public string AuditTargetType => "RegulatoryReport";
    public string? AuditTargetId => ReportId.ToString();
}

public class SubmitReportToEfccCommandHandler(
    IRegulatoryReportRepository reports,
    INccReportingClient nccClient)
    : IRequestHandler<SubmitReportToEfccCommand, Result>
{
    public async Task<Result> Handle(SubmitReportToEfccCommand cmd, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(cmd.ReportId, ct);
        if (report is null || report.TenantId != cmd.TenantId)
            return Result.Failure("Report not found.");

        if (report.SubmittedAt.HasValue)
            return Result.Failure("Report already submitted.");

        if (report.Status != ReportStatus.Generated)
            return Result.Failure("Only generated reports can be submitted.");

        // Re-use the NCC client for EFCC submission path (same regulatory API)
        var reference = await nccClient.SubmitAsync(report.DataJson, ct);
        report.MarkSubmitted(reference);
        return Result.Success();
    }
}
