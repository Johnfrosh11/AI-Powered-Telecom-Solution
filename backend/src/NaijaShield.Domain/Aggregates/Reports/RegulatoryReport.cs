using NaijaShield.Domain.Common;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Domain.Aggregates.Reports;

/// <summary>A generated regulatory report (EFCC, CBN, NCC).</summary>
public class RegulatoryReport : AggregateRoot<Guid>
{
    public ReportType Type { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public ReportStatus Status { get; private set; }
    public Guid GeneratedByUserId { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public string DataJson { get; private set; } = string.Empty;
    public string OutputBlobUri { get; private set; } = string.Empty;
    public DateTime? SubmittedAt { get; private set; }
    public string? RegulatorReference { get; private set; }
    public Guid TenantId { get; private set; }

    private RegulatoryReport() { }

    public static RegulatoryReport Create(
        Guid tenantId,
        ReportType type,
        DateTime periodStart,
        DateTime periodEnd,
        Guid generatedByUserId)
    {
        return new RegulatoryReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = DateTime.UtcNow,
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SetOutput(string dataJson, string outputBlobUri)
    {
        DataJson = dataJson;
        OutputBlobUri = outputBlobUri;
        Status = ReportStatus.Generated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSubmitted(string regulatorReference)
    {
        Status = ReportStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        RegulatorReference = regulatorReference;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Acknowledge(string regulatorReference)
    {
        Status = ReportStatus.Acknowledged;
        RegulatorReference = regulatorReference;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = ReportStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>Configuration for an automatically-scheduled recurring report.</summary>
public class ScheduledReport : AggregateRoot<Guid>
{
    public ReportType Type { get; set; }

    /// <summary>Cron expression (Africa/Lagos timezone).</summary>
    public string CronExpression { get; set; } = string.Empty;

    public string[] Recipients { get; set; } = [];
    public string DeliveryChannel { get; set; } = "Email"; // Email | Slack | Teams
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}
