namespace NaijaShield.Domain.Enums;

public enum Channel { Voice, Sms, WhatsApp, Ussd }

public enum ScamCallStatus { Detected, Confirmed, FalsePositive, Reported, Resolved }

public enum ScamSeverity { Low, Medium, High, Critical }

public enum WatchlistStatus { Monitored, Blocked, ReportedToEFCC, Whitelisted }

public enum WarningChannel { Sms, WhatsApp, Voice }

public enum WarningStatus { Pending, Sent, Delivered, Failed }

public enum ConversationStatus { Open, Escalated, Closed, ScamFlagged }

public enum MessageType { Text, Audio, System, Translation }

public enum ReportType
{
    EFCC,
    CBN,
    NccQoS,
    NccComplaints,
    InternalExecutive,
    CostSavings
}

public enum ReportStatus { Draft, Generated, Submitted, Acknowledged, Failed }

public enum AuditResult { Success, Failure, Denied }

public enum AuditSensitivity { Low, Medium, High, Critical }

public enum CustomerStatus { Active, Barred, Suspended, Churned }

public enum InteractionStatus { Open, Resolved, Escalated, Abandoned }

public enum TenantStatus { Active, Suspended, Trial }

public enum SubscriptionPlan { Trial, Starter, Enterprise, Custom }

public enum IntegrationCategory
{
    TelcoCore,
    Channel,
    AICloud,
    Government,
    Observability
}

public enum ExportRequestStatus { Pending, Processing, Ready, Failed }
