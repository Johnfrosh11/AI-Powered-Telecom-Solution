namespace NaijaShield.Domain.Constants;

/// <summary>
/// All permission codes used for RBAC.  Pattern: module.resource.action.
/// These must never be magic strings — always reference this class.
/// </summary>
public static class Permissions
{
    // ── Fraud ──────────────────────────────────────────────────────────────
    public const string FraudCallsView = "fraud.calls.view";
    public const string FraudCallsBlock = "fraud.calls.block";
    public const string FraudCallsExport = "fraud.calls.export";
    public const string FraudCallsConfirm = "fraud.calls.confirm";
    public const string FraudCallsMarkFalsePositive = "fraud.calls.mark_false_positive";
    public const string FraudPatternsView = "fraud.patterns.view";
    public const string FraudPatternsManage = "fraud.patterns.manage";
    public const string FraudWatchlistView = "fraud.watchlist.view";
    public const string FraudWatchlistManage = "fraud.watchlist.manage";
    public const string FraudReportsSubmitEfcc = "fraud.reports.submit_efcc";
    public const string FraudWarningsSend = "fraud.warnings.send";

    // ── Network ────────────────────────────────────────────────────────────
    public const string NetworkOutagesView = "network.outages.view";
    public const string NetworkOutagesAcknowledge = "network.outages.acknowledge";
    public const string NetworkAutoresponseConfigure = "network.autoresponse.configure";

    // ── Customers ──────────────────────────────────────────────────────────
    public const string CustomersView = "customers.view";
    public const string CustomersViewPii = "customers.view_pii";
    public const string CustomersExport = "customers.export";

    // ── Conversations ──────────────────────────────────────────────────────
    public const string ConversationsView = "conversations.view";
    public const string ConversationsTakeover = "conversations.takeover";
    public const string ConversationsClose = "conversations.close";

    // ── AI ─────────────────────────────────────────────────────────────────
    public const string AiPromptsView = "ai.prompts.view";
    public const string AiPromptsEdit = "ai.prompts.edit";
    public const string AiModelsRetrain = "ai.models.retrain";
    public const string AiSkillsManage = "ai.skills.manage";
    public const string AiSandboxUse = "ai.sandbox.use";

    // ── Reports ────────────────────────────────────────────────────────────
    public const string ReportsView = "reports.view";
    public const string ReportsCreate = "reports.create";
    public const string ReportsSubmitNcc = "reports.submit_ncc";
    public const string ReportsSubmitCbn = "reports.submit_cbn";

    // ── Users ──────────────────────────────────────────────────────────────
    public const string UsersView = "users.view";
    public const string UsersInvite = "users.invite";
    public const string UsersDeactivate = "users.deactivate";
    public const string UsersBulkImport = "users.bulk_import";

    // ── Roles ──────────────────────────────────────────────────────────────
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";

    // ── SSO ────────────────────────────────────────────────────────────────
    public const string SsoMappingsManage = "sso.mappings.manage";

    // ── API Keys ───────────────────────────────────────────────────────────
    public const string ApiKeysView = "api_keys.view";
    public const string ApiKeysManage = "api_keys.manage";

    // ── Settings ───────────────────────────────────────────────────────────
    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";

    // ── Integrations ───────────────────────────────────────────────────────
    public const string IntegrationsView = "integrations.view";
    public const string IntegrationsManage = "integrations.manage";

    // ── Audit ──────────────────────────────────────────────────────────────
    public const string AuditView = "audit.view";
    public const string AuditExport = "audit.export";

    // ── Data ───────────────────────────────────────────────────────────────
    public const string DataRetentionManage = "data.retention.manage";
    public const string DataExportCreate = "data.export.create";

    // ── Feature Flags ──────────────────────────────────────────────────────
    public const string FeatureFlagsManage = "feature_flags.manage";

    // ── Webhooks ───────────────────────────────────────────────────────────
    public const string WebhooksManage = "webhooks.manage";

    /// <summary>Returns every defined permission code.</summary>
    public static IReadOnlyList<string> All =>
    [
        FraudCallsView, FraudCallsBlock, FraudCallsExport, FraudCallsConfirm,
        FraudCallsMarkFalsePositive, FraudPatternsView, FraudPatternsManage,
        FraudWatchlistView, FraudWatchlistManage, FraudReportsSubmitEfcc,
        FraudWarningsSend, NetworkOutagesView, NetworkOutagesAcknowledge,
        NetworkAutoresponseConfigure, CustomersView, CustomersViewPii,
        CustomersExport, ConversationsView, ConversationsTakeover,
        ConversationsClose, AiPromptsView, AiPromptsEdit, AiModelsRetrain,
        AiSkillsManage, AiSandboxUse, ReportsView, ReportsCreate, ReportsSubmitNcc,
        ReportsSubmitCbn, UsersView, UsersInvite, UsersDeactivate, UsersBulkImport,
        RolesView, RolesManage, SsoMappingsManage, ApiKeysView, ApiKeysManage,
        SettingsView, SettingsManage, IntegrationsView, IntegrationsManage,
        AuditView, AuditExport, DataRetentionManage, DataExportCreate,
        FeatureFlagsManage, WebhooksManage
    ];
}

/// <summary>System role names — never change these strings once seeded.</summary>
public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string FraudAnalyst = "FraudAnalyst";
    public const string NetworkEngineer = "NetworkEngineer";
    public const string CSSupervisor = "CSSupervisor";
    public const string ComplianceOfficer = "ComplianceOfficer";
    public const string Auditor = "Auditor";
    public const string ReadOnly = "ReadOnly";
}

/// <summary>Queue / topic names for Azure Service Bus.</summary>
public static class ServiceBusTopics
{
    public const string DomainEvents = "naijashield-domain-events";
    public const string CdrIngestion = "naijashield-cdr-ingestion";
    public const string OutboxMessages = "naijashield-outbox";
    public const string ScamAlerts = "naijashield-scam-alerts";
    public const string RegulatoryReports = "naijashield-regulatory-reports";
}

/// <summary>Blob container names.</summary>
public static class BlobContainers
{
    public const string CallRecordings = "call-recordings";
    public const string ReportExports = "report-exports";
    public const string DataExports = "data-exports";
    public const string AuditExports = "audit-exports";
}

/// <summary>Cache key prefixes.</summary>
public static class CacheKeys
{
    public const string UserPermissions = "user-perms:";
    public const string DashboardKpis = "dashboard-kpis:";
    public const string TenantSettings = "tenant-settings:";
    public const string PromptTemplates = "prompt-templates:";
    public const string FeatureFlags = "feature-flags:";
}
