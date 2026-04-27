using FluentValidation;
using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Reports;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Application.Features.Reports;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ReportDto(
    Guid Id, string Type, DateTime PeriodStart, DateTime PeriodEnd,
    string Status, DateTime GeneratedAt, string OutputBlobUri,
    DateTime? SubmittedAt, string? RegulatorReference, Guid TenantId);

// ── Generate Report Command ───────────────────────────────────────────────────

public record GenerateReportCommand(
    Guid TenantId,
    Guid RequestedByUserId,
    string ReportType,
    DateTime PeriodStart,
    DateTime PeriodEnd)
    : IRequest<Result<Guid>>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "reports.create";
    public string AuditTargetType => "RegulatoryReport";
    public string? AuditTargetId => null;
}

public class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    public GenerateReportCommandValidator()
    {
        RuleFor(x => x.ReportType)
            .NotEmpty()
            .Must(t => Enum.TryParse<ReportType>(t, out _))
            .WithMessage("Invalid report type.");
        RuleFor(x => x.PeriodStart).LessThan(x => x.PeriodEnd);
    }
}

public class GenerateReportCommandHandler(
    IRegulatoryReportRepository reports,
    IScamDetectionAiService aiService,
    IAzureBlobStorage blobStorage)
    : IRequestHandler<GenerateReportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GenerateReportCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<ReportType>(cmd.ReportType, out var type))
            return Result.Failure<Guid>("Invalid report type.");

        var report = RegulatoryReport.Create(
            cmd.TenantId, type, cmd.PeriodStart, cmd.PeriodEnd, cmd.RequestedByUserId);

        // Generate executive brief
        var content = await aiService.GenerateExecutiveBriefAsync(cmd.PeriodStart, cmd.PeriodEnd, cmd.TenantId, ct);
        var dataJson = System.Text.Json.JsonSerializer.Serialize(new { content, generatedAt = DateTime.UtcNow });

        // Upload to blob
        var blobName = $"reports/{cmd.TenantId}/{report.Id}/{type}.json";
        var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(dataJson));
        var blobUri = await blobStorage.UploadAsync(stream, "report-exports", blobName, "application/json", ct);

        report.SetOutput(dataJson, blobUri);
        await reports.AddAsync(report, ct);

        return Result.Success(report.Id);
    }
}

// ── Submit To NCC ─────────────────────────────────────────────────────────────

public record SubmitReportToNccCommand(Guid ReportId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "reports.submit_ncc";
    public string AuditTargetType => "RegulatoryReport";
    public string? AuditTargetId => ReportId.ToString();
    public string AuditSensitivity => "High";
}

public class SubmitReportToNccCommandHandler(
    IRegulatoryReportRepository reports,
    INccReportingClient nccClient)
    : IRequestHandler<SubmitReportToNccCommand, Result>
{
    public async Task<Result> Handle(SubmitReportToNccCommand cmd, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(cmd.ReportId, ct);
        if (report is null || report.TenantId != cmd.TenantId)
            return Result.Failure("Report not found.");

        if (report.Status != ReportStatus.Generated)
            return Result.Failure("Only generated reports can be submitted.");

        var reference = await nccClient.SubmitAsync(report.DataJson, ct);
        report.MarkSubmitted(reference);
        return Result.Success();
    }
}

// ── List Reports Query ────────────────────────────────────────────────────────

public record ListReportsQuery(Guid TenantId, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<ReportDto>>>;

public class ListReportsQueryHandler(IRegulatoryReportRepository reports)
    : IRequestHandler<ListReportsQuery, Result<PagedResult<ReportDto>>>
{
    public async Task<Result<PagedResult<ReportDto>>> Handle(ListReportsQuery q, CancellationToken ct)
    {
        var items = await reports.ListAsync(q.TenantId, q.Page, q.PageSize, ct);
        var total = await reports.CountAsync(q.TenantId, ct);

        var dtos = items.Select(r => new ReportDto(
            r.Id, r.Type.ToString(), r.PeriodStart, r.PeriodEnd,
            r.Status.ToString(), r.GeneratedAt, r.OutputBlobUri,
            r.SubmittedAt, r.RegulatorReference, r.TenantId)).ToList();

        return Result.Success(new PagedResult<ReportDto>(dtos, total, q.Page, q.PageSize));
    }
}
