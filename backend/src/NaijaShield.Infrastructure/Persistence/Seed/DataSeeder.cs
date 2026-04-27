using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Aggregates.ScamDetection;
using NaijaShield.Domain.Aggregates.Tenants;
using NaijaShield.Domain.Constants;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the database with system roles, permissions, default scam patterns,
/// prompt templates per language, and a demo admin tenant + user.
/// Safe to run multiple times (idempotent).
/// </summary>
public class DataSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<DataSeeder> logger)
{
    // Well-known GUIDs so seeds are stable across environments
    private static readonly Guid SystemTenantId   = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MtnTenantId      = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid AirtelTenantId   = new("10000000-0000-0000-0000-000000000002");
    private static readonly Guid SystemAdminUserId = new("00000000-0000-0000-0001-000000000001");
    private static readonly Guid MtnAdminUserId   = new("10000000-0000-0001-0000-000000000001");

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        await SeedPermissionsAsync(ct);
        await SeedTenantsAsync(ct);
        await SeedRolesAsync(ct);
        await SeedAdminUsersAsync(ct);
        await SeedScamPatternsAsync(ct);
        await SeedPromptTemplatesAsync(ct);

        logger.LogInformation("Data seeding completed");
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        if (await db.Permissions.AnyAsync(ct)) return;

        var all = AllPermissions();
        db.Permissions.AddRange(all);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} permissions", all.Count);
    }

    private static List<PermissionEntry> AllPermissions()
    {
        static PermissionEntry P(string code, string display, string module) =>
            new() { Id = Guid.NewGuid(), Code = code, DisplayName = display, Module = module };

        return [
            // Fraud
            P(Permissions.FraudCallsView,              "View Fraud Calls",            "Fraud"),
            P(Permissions.FraudCallsBlock,             "Block Calls",                 "Fraud"),
            P(Permissions.FraudCallsExport,            "Export Fraud Calls",          "Fraud"),
            P(Permissions.FraudCallsConfirm,           "Confirm Fraud Calls",         "Fraud"),
            P(Permissions.FraudCallsMarkFalsePositive, "Mark False Positive",         "Fraud"),
            P(Permissions.FraudPatternsView,           "View Scam Patterns",          "Fraud"),
            P(Permissions.FraudPatternsManage,         "Manage Scam Patterns",        "Fraud"),
            P(Permissions.FraudWatchlistView,          "View Watchlist",              "Fraud"),
            P(Permissions.FraudWatchlistManage,        "Manage Watchlist",            "Fraud"),
            P(Permissions.FraudReportsSubmitEfcc,      "Submit Report to EFCC",       "Fraud"),
            P(Permissions.FraudWarningsSend,           "Send Scam Warnings",          "Fraud"),
            // Network
            P(Permissions.NetworkOutagesView,          "View Network Outages",        "Network"),
            P(Permissions.NetworkOutagesAcknowledge,   "Acknowledge Outages",         "Network"),
            P(Permissions.NetworkAutoresponseConfigure,"Configure Auto-Response",     "Network"),
            // Customers
            P(Permissions.CustomersView,               "View Customers",              "Customers"),
            P(Permissions.CustomersViewPii,            "View Customer PII",           "Customers"),
            P(Permissions.CustomersExport,             "Export Customers",            "Customers"),
            // Conversations
            P(Permissions.ConversationsView,           "View Conversations",          "Conversations"),
            P(Permissions.ConversationsTakeover,       "Takeover Conversations",      "Conversations"),
            P(Permissions.ConversationsClose,          "Close Conversations",         "Conversations"),
            // AI
            P(Permissions.AiPromptsView,               "View AI Prompts",             "AI"),
            P(Permissions.AiPromptsEdit,               "Edit AI Prompts",             "AI"),
            P(Permissions.AiModelsRetrain,             "Retrain AI Models",           "AI"),
            P(Permissions.AiSkillsManage,              "Manage AI Skills",            "AI"),
            P(Permissions.AiSandboxUse,                "Use AI Sandbox",              "AI"),
            // Reports
            P(Permissions.ReportsView,                 "View Reports",                "Reports"),
            P(Permissions.ReportsCreate,               "Create Reports",              "Reports"),
            P(Permissions.ReportsSubmitNcc,            "Submit Report to NCC",        "Reports"),
            P(Permissions.ReportsSubmitCbn,            "Submit Report to CBN",        "Reports"),
            // Users
            P(Permissions.UsersView,                   "View Users",                  "Users"),
            P(Permissions.UsersInvite,                 "Invite Users",                "Users"),
            P(Permissions.UsersDeactivate,             "Deactivate Users",            "Users"),
            P(Permissions.UsersBulkImport,             "Bulk Import Users",           "Users"),
            // Roles
            P(Permissions.RolesView,                   "View Roles",                  "Roles"),
            P(Permissions.RolesManage,                 "Manage Roles",                "Roles"),
            // SSO
            P(Permissions.SsoMappingsManage,           "Manage SSO Mappings",         "SSO"),
            // API Keys
            P(Permissions.ApiKeysView,                 "View API Keys",               "APIKeys"),
            P(Permissions.ApiKeysManage,               "Manage API Keys",             "APIKeys"),
            // Settings
            P(Permissions.SettingsView,                "View Settings",               "Settings"),
            P(Permissions.SettingsManage,              "Manage Settings",             "Settings"),
            // Integrations
            P(Permissions.IntegrationsView,            "View Integrations",           "Integrations"),
            P(Permissions.IntegrationsManage,          "Manage Integrations",         "Integrations"),
            // Audit
            P(Permissions.AuditView,                   "View Audit Logs",             "Audit"),
            P(Permissions.AuditExport,                 "Export Audit Logs",           "Audit"),
            // Data
            P(Permissions.DataRetentionManage,         "Manage Data Retention",       "Data"),
            P(Permissions.DataExportCreate,            "Create Data Exports",         "Data"),
            // Feature Flags
            P(Permissions.FeatureFlagsManage,          "Manage Feature Flags",        "System"),
            P(Permissions.WebhooksManage,              "Manage Webhooks",             "System"),
        ];
    }

    // ── Tenants ───────────────────────────────────────────────────────────────

    private async Task SeedTenantsAsync(CancellationToken ct)
    {
        if (await db.Tenants.AnyAsync(ct)) return;

        var tenants = new[]
        {
            Tenant.Create("MTN Nigeria", "NCC-MTN-001", "https://cdn.naijashield.ai/logos/mtn.png", SubscriptionPlan.Enterprise),
            Tenant.Create("Airtel Nigeria", "NCC-AIR-002", "https://cdn.naijashield.ai/logos/airtel.png", SubscriptionPlan.Enterprise),
            Tenant.Create("Glo Mobile", "NCC-GLO-003", "https://cdn.naijashield.ai/logos/glo.png", SubscriptionPlan.Starter),
            Tenant.Create("9mobile", "NCC-9MB-004", "https://cdn.naijashield.ai/logos/9mobile.png", SubscriptionPlan.Starter),
        };

        // Override IDs for stability using reflection (private set)
        SetId(tenants[0], MtnTenantId);
        SetId(tenants[1], AirtelTenantId);

        db.Tenants.AddRange(tenants);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} tenants", tenants.Length);
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(ct)) return;

        var permMap = await db.Permissions
            .ToDictionaryAsync(p => p.Code, p => p.Id, ct);

        // Helper: get permission IDs for a list of codes
        List<Guid> Pids(params string[] codes) =>
            codes.Where(permMap.ContainsKey).Select(c => permMap[c]).ToList();

        var allIds = permMap.Values.ToArray();

        var roles = new List<Role>();

        void AddRole(Guid tenantId, string name, string display, string desc, Guid[] permIds)
        {
            var role = Role.Create(tenantId, name, display, desc, isSystemRole: true);
            role.SetPermissions(permIds);
            roles.Add(role);
        }

        foreach (var tenantId in new[] { MtnTenantId, AirtelTenantId })
        {
            AddRole(tenantId, "Admin", "Administrator", "Full access to all features", allIds);

            AddRole(tenantId, "FraudAnalyst", "Fraud Analyst", "Fraud detection and management",
                Pids(Permissions.FraudCallsView, Permissions.FraudCallsBlock, Permissions.FraudCallsExport,
                     Permissions.FraudCallsConfirm, Permissions.FraudCallsMarkFalsePositive,
                     Permissions.FraudPatternsView, Permissions.FraudPatternsManage,
                     Permissions.FraudWatchlistView, Permissions.FraudWatchlistManage,
                     Permissions.FraudReportsSubmitEfcc, Permissions.FraudWarningsSend,
                     Permissions.CustomersView, Permissions.ConversationsView,
                     Permissions.ReportsView, Permissions.AiSandboxUse).ToArray());

            AddRole(tenantId, "NetworkEngineer", "Network Engineer", "Network monitoring and management",
                Pids(Permissions.NetworkOutagesView, Permissions.NetworkOutagesAcknowledge,
                     Permissions.NetworkAutoresponseConfigure,
                     Permissions.CustomersView, Permissions.ReportsView).ToArray());

            AddRole(tenantId, "CSSupervisor", "CS Supervisor", "Customer service oversight",
                Pids(Permissions.ConversationsView, Permissions.ConversationsTakeover, Permissions.ConversationsClose,
                     Permissions.CustomersView, Permissions.CustomersViewPii,
                     Permissions.AiPromptsView).ToArray());

            AddRole(tenantId, "ComplianceOfficer", "Compliance Officer", "Regulatory compliance",
                Pids(Permissions.FraudCallsView, Permissions.FraudReportsSubmitEfcc,
                     Permissions.ReportsView, Permissions.ReportsCreate,
                     Permissions.ReportsSubmitNcc, Permissions.ReportsSubmitCbn,
                     Permissions.AuditView, Permissions.AuditExport,
                     Permissions.DataRetentionManage).ToArray());

            AddRole(tenantId, "Auditor", "Auditor", "Read-only audit access",
                Pids(Permissions.FraudCallsView, Permissions.FraudPatternsView,
                     Permissions.FraudWatchlistView, Permissions.NetworkOutagesView,
                     Permissions.CustomersView, Permissions.ConversationsView,
                     Permissions.ReportsView, Permissions.UsersView, Permissions.RolesView,
                     Permissions.SettingsView, Permissions.IntegrationsView,
                     Permissions.AuditView, Permissions.AuditExport).ToArray());

            AddRole(tenantId, "ReadOnly", "Read Only", "View-only access",
                permMap.Keys.Where(k => k.EndsWith(".view")).Select(k => permMap[k]).ToArray());
        }

        db.Roles.AddRange(roles);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} roles", roles.Count);
    }

    // ── Admin Users ───────────────────────────────────────────────────────────

    private async Task SeedAdminUsersAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct)) return;

        var mtnAdminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.TenantId == MtnTenantId && r.Name == "Admin", ct);

        var mtnAdmin = AppUser.Create(
            MtnTenantId,
            "admin@mtn.naijashield.ai",
            "MTN Admin",
            preferredLanguage: "en");

        SetId(mtnAdmin, MtnAdminUserId);
        mtnAdmin.SetPasswordHash(passwordHasher.Hash("NaijaShield@2024!")); // change on first login
        if (mtnAdminRole != null)
            mtnAdmin.AssignRole(mtnAdminRole.Id, MtnAdminUserId);

        db.Users.Add(mtnAdmin);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded admin user: {Email}", mtnAdmin.Email);
    }

    // ── Scam Patterns ─────────────────────────────────────────────────────────

    private async Task SeedScamPatternsAsync(CancellationToken ct)
    {
        if (await db.ScamPatterns.AnyAsync(ct)) return;

        foreach (var tenantId in new[] { MtnTenantId, AirtelTenantId })
        {
            var patterns = BuildScamPatterns(tenantId);
            db.ScamPatterns.AddRange(patterns);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded scam patterns for {Count} tenants", 2);
    }

    private static List<ScamPattern> BuildScamPatterns(Guid tenantId)
    {
        var patterns = new List<ScamPattern>();

        // 1. Bank Impersonation (OTP/Account Scam)
        var bankImpersonation = ScamPattern.Create(
            tenantId,
            "Bank OTP Scam",
            "Caller impersonates bank staff requesting OTP, PIN or BVN to 'secure account'",
            "Financial Fraud",
            ScamSeverity.Critical);
        bankImpersonation.AddPhrase("en",     "please send us your OTP",                         0.9m);
        bankImpersonation.AddPhrase("en",     "your account will be blocked unless you verify",  0.85m);
        bankImpersonation.AddPhrase("pidgin", "abeg give me your OTP make I help you",           0.9m);
        bankImpersonation.AddPhrase("yo",     "jowo fun wa ni OTP rẹ",                           0.88m);
        bankImpersonation.AddPhrase("ha",     "don Allah ka ba mu OTP ɗinka",                   0.87m);
        bankImpersonation.AddPhrase("ig",     "biko nye anyị OTP gị",                           0.88m);
        patterns.Add(bankImpersonation);

        // 2. Lottery / Prize Scam
        var lottery = ScamPattern.Create(
            tenantId,
            "Lottery Prize Scam",
            "Caller claims victim won a prize and demands upfront fee or personal details",
            "Advance Fee Fraud",
            ScamSeverity.High);
        lottery.AddPhrase("en",     "you have won one million naira",                  0.95m);
        lottery.AddPhrase("en",     "pay processing fee to claim your prize",          0.9m);
        lottery.AddPhrase("pidgin", "you don win big money, send small money first",   0.92m);
        lottery.AddPhrase("yo",     "o ti bori ẹ̀bùn, san owó ìtọ́sí",                0.88m);
        lottery.AddPhrase("ha",     "ka biya kuɗi kaɗan don karɓar kyautarku",        0.87m);
        lottery.AddPhrase("ig",     "ị meriri, were ego obere zipụta ego ukwu",        0.88m);
        patterns.Add(lottery);

        // 3. SIM Swap / Phone Hijack
        var simSwap = ScamPattern.Create(
            tenantId,
            "SIM Swap Attack",
            "Caller tricks subscriber into revealing info to enable unauthorised SIM swap",
            "Identity Fraud",
            ScamSeverity.Critical);
        simSwap.AddPhrase("en",     "we need to verify your NIN for SIM upgrade",      0.92m);
        simSwap.AddPhrase("en",     "dial star hash to confirm your SIM",              0.85m);
        simSwap.AddPhrase("pidgin", "dial code confirm your line make dem no block am", 0.9m);
        simSwap.AddPhrase("yo",     "pe nọmba náà láti jẹ́rìí sí ìrísí SIM rẹ",       0.87m);
        simSwap.AddPhrase("ha",     "kira lambar tabbatar da SIM ɗinku",               0.86m);
        simSwap.AddPhrase("ig",     "kpọọ nọmba iji gosipụta SIM gị",                 0.87m);
        patterns.Add(simSwap);

        // 4. Investment / Ponzi Scheme
        var investment = ScamPattern.Create(
            tenantId,
            "Fake Investment Scheme",
            "Caller promises unrealistic returns on investments in cryptocurrency or forex",
            "Investment Fraud",
            ScamSeverity.High);
        investment.AddPhrase("en",     "guaranteed 50% return in 7 days",              0.93m);
        investment.AddPhrase("en",     "invest in our crypto platform today",          0.88m);
        investment.AddPhrase("pidgin", "e go double your money within one week",       0.91m);
        investment.AddPhrase("yo",     "owó rẹ yóò pọ̀ sí ilọpo méjì ní ọsẹ kan",    0.88m);
        investment.AddPhrase("ha",     "kuɗinku zai ninkatawa ninki biyu cikin kwana bakwai", 0.87m);
        investment.AddPhrase("ig",     "ego gị ga abawanye ugboro abụọ n'ime izu asaa", 0.88m);
        patterns.Add(investment);

        // 5. Government Impersonation (EFCC/CBN/NCC)
        var govImpersonation = ScamPattern.Create(
            tenantId,
            "Government Agency Impersonation",
            "Caller impersonates EFCC, CBN, NCC, or police officials to extort money",
            "Authority Fraud",
            ScamSeverity.Critical);
        govImpersonation.AddPhrase("en",     "this is EFCC calling about your account",       0.93m);
        govImpersonation.AddPhrase("en",     "you will be arrested unless you pay",            0.9m);
        govImpersonation.AddPhrase("pidgin", "na EFCC we be, you go enter prison if you no pay", 0.92m);
        govImpersonation.AddPhrase("yo",     "a máa mú ọ tí o bá san owó tó pọ̀",            0.87m);
        govImpersonation.AddPhrase("ha",     "za a kama ku sai dai kun biya",                 0.86m);
        govImpersonation.AddPhrase("ig",     "a ga-ejide gị ọ bụrụ na ị na-akwụghị ụgwọ",   0.87m);
        patterns.Add(govImpersonation);

        // 6. Romance / Dating Scam
        var romance = ScamPattern.Create(
            tenantId,
            "Romance Scam",
            "Scammer builds fake romantic relationship then requests money for emergency",
            "Social Engineering",
            ScamSeverity.Medium);
        romance.AddPhrase("en",     "I am stuck abroad and need money urgently",        0.85m);
        romance.AddPhrase("en",     "send me airtime so we can keep talking",           0.8m);
        romance.AddPhrase("pidgin", "I need money to come see you, send quickly",       0.88m);
        romance.AddPhrase("yo",     "fi owó ránṣẹ́ sí mi kí n lè bọ̀ wá bẹ ọ wò",    0.83m);
        romance.AddPhrase("ha",     "aika mini kuɗi zan iya zuwa ganku",                0.82m);
        romance.AddPhrase("ig",     "zipu m ego ka m were bia hụ gị",                  0.83m);
        patterns.Add(romance);

        // 7. Job Scam
        var job = ScamPattern.Create(
            tenantId,
            "Fake Job Offer",
            "Caller offers high-paying jobs abroad but demands processing fees or NIN/BVN",
            "Recruitment Fraud",
            ScamSeverity.High);
        job.AddPhrase("en",     "you have been selected for a job in Dubai",            0.88m);
        job.AddPhrase("en",     "pay registration fee to process your documents",       0.87m);
        job.AddPhrase("pidgin", "we don select you for big job abroad, just pay small fee", 0.9m);
        job.AddPhrase("yo",     "a yàn ọ fún iṣẹ́ nílẹ̀ òkè àárọ̀, san owó ìforúkọsílẹ̀", 0.85m);
        job.AddPhrase("ha",     "an zaɓe ku don aiki a ƙasashen waje",                 0.84m);
        job.AddPhrase("ig",     "a họrọ gị maka ọrụ n'ebe ọzọ, kwụọ ụgwọ ntinye aha", 0.85m);
        patterns.Add(job);

        // 8. Fake Loan Offer
        var loan = ScamPattern.Create(
            tenantId,
            "Fake Loan Offer",
            "Caller offers instant loans but collects upfront insurance or processing fees",
            "Financial Fraud",
            ScamSeverity.High);
        loan.AddPhrase("en",     "get instant loan of 500,000 naira today",             0.87m);
        loan.AddPhrase("en",     "pay insurance fee to unlock your loan",               0.9m);
        loan.AddPhrase("pidgin", "we go give you loan today today, just pay small fee", 0.91m);
        loan.AddPhrase("yo",     "gba àárò awin lẹ́sẹ̀kẹsẹ̀, san owó ìnśúráǹsì díẹ̀", 0.86m);
        loan.AddPhrase("ha",     "samun aron nan take, amma a biya inshorar farko",     0.85m);
        loan.AddPhrase("ig",     "nweta ngwa ọszọ taa, kwụọ ụgwọ nchedo na-izute",    0.86m);
        patterns.Add(loan);

        // 9. Recharge Card / Airtime Scam
        var airtime = ScamPattern.Create(
            tenantId,
            "Recharge Card Scam",
            "Victim tricked into buying airtime/gift cards and revealing pin codes",
            "Payment Fraud",
            ScamSeverity.Medium);
        airtime.AddPhrase("en",     "buy recharge card and scratch the PIN for me",     0.92m);
        airtime.AddPhrase("en",     "send me iTunes gift card to claim your prize",     0.9m);
        airtime.AddPhrase("pidgin", "buy MTN card scratch the number text am for me",  0.93m);
        airtime.AddPhrase("yo",     "ra káàdì afẹ̀núwò tẹlẹ, fi nọ́mbà rẹ̀ ránṣẹ́",  0.89m);
        airtime.AddPhrase("ha",     "sayi katin caja ka rubuta PIN ka aiko mini",       0.88m);
        airtime.AddPhrase("ig",     "zụọ kaadị ụgwọ ikpo oyi, ziga m nọmba ya",        0.89m);
        patterns.Add(airtime);

        // 10. Technical Support Scam
        var techSupport = ScamPattern.Create(
            tenantId,
            "Fake Technical Support",
            "Caller pretends to be telco tech support and requests remote access or fees",
            "Tech Support Fraud",
            ScamSeverity.Medium);
        techSupport.AddPhrase("en",     "your phone has been hacked, allow us remote access", 0.88m);
        techSupport.AddPhrase("en",     "pay for antivirus to protect your SIM card",          0.86m);
        techSupport.AddPhrase("pidgin", "your phone don hack, give us code make we fix am",    0.9m);
        techSupport.AddPhrase("yo",     "fóònù rẹ ti jẹ gbe, fún wa ní ìráàwọ",              0.85m);
        techSupport.AddPhrase("ha",     "an kutsa cikin wayarka, bamu damar shiga ta nisa",    0.84m);
        techSupport.AddPhrase("ig",     "eti n'ime ekwentị gị, nye anyị ohere ịbanye ya",     0.85m);
        patterns.Add(techSupport);

        return patterns;
    }

    // ── Prompt Templates ──────────────────────────────────────────────────────

    private async Task SeedPromptTemplatesAsync(CancellationToken ct)
    {
        if (await db.PromptTemplates.AnyAsync(ct)) return;

        var templates = new List<PromptTemplate>();

        foreach (var tenantId in new[] { MtnTenantId, AirtelTenantId })
        {
            templates.AddRange(BuildPromptTemplates(tenantId));
        }

        db.PromptTemplates.AddRange(templates);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} prompt templates", templates.Count);
    }

    private static List<PromptTemplate> BuildPromptTemplates(Guid tenantId)
    {
        var templates = new List<PromptTemplate>();
        var systemUserId = SystemAdminUserId;

        PromptTemplate T(string name, string lang, string useCase, string content) =>
            PromptTemplate.Create(tenantId, systemUserId, name, lang, useCase, content);

        // English scam detection
        templates.Add(T(
            "Scam Detection — English",
            "en",
            "scam_detection",
            """
            You are an expert Nigerian telecoms fraud analyst. Analyse the following call transcript and determine if it is a scam.

            TRANSCRIPT:
            {{transcript}}

            KNOWN PATTERNS:
            {{patterns}}

            Respond with a JSON object:
            {
              "isScam": true/false,
              "confidence": 0.0-1.0,
              "category": "<fraud category or null>",
              "reasoning": "<chain-of-thought explanation>",
              "triggerPhrases": ["<matched phrase 1>", ...],
              "urgencyMarkers": ["<urgency phrase>", ...],
              "requestedAction": "<what victim was asked to do>"
            }

            Be especially vigilant for: OTP requests, NIN/BVN requests, upfront fees, impersonation of EFCC/CBN/NCC, prize claims.
            """));

        // Nigerian Pidgin scam detection
        templates.Add(T(
            "Scam Detection — Nigerian Pidgin",
            "pidgin",
            "scam_detection",
            """
            You are an expert Nigerian telecoms fraud analyst who understands Nigerian Pidgin English deeply.
            Analyse this call transcript in Nigerian Pidgin and identify potential scam content.

            TRANSCRIPT (Nigerian Pidgin):
            {{transcript}}

            KNOWN PATTERNS:
            {{patterns}}

            Key Pidgin scam indicators to watch for:
            - "OTP" requests phrased as "send am" or "give me the number"
            - "Make I help you" before requesting sensitive info
            - References to "win big money" or "your account go block"
            - Urgency phrases: "do am now now", "quick quick", "before dem block am"

            Respond with a JSON object:
            {
              "isScam": true/false,
              "confidence": 0.0-1.0,
              "category": "<fraud category or null>",
              "reasoning": "<explanation in English>",
              "triggerPhrases": ["<matched Pidgin phrase>", ...],
              "requestedAction": "<what victim was asked to do>"
            }
            """));

        // Yoruba scam detection
        templates.Add(T(
            "Scam Detection — Yoruba",
            "yo",
            "scam_detection",
            """
            You are a Nigerian telecoms fraud analyst fluent in Yoruba.
            Analyse this Yoruba transcript for scam activity. Yoruba scammers often use formal, respectful language (with "ẹ" honorifics) to gain trust.

            TRANSCRIPT (Yoruba):
            {{transcript}}

            KNOWN PATTERNS:
            {{patterns}}

            Watch for Yoruba scam indicators:
            - "jowo" (please) before unusual requests
            - References to "ẹbùn" (prize/gift) requiring payment
            - Authority impersonation: "Ọlọpaa", "CBN", "EFCC"
            - OTP requests: "nọmba tó jẹ́ ìrísí rẹ"

            Respond with JSON:
            {
              "isScam": true/false,
              "confidence": 0.0-1.0,
              "category": "<fraud category>",
              "reasoning": "<English explanation>",
              "triggerPhrases": ["<Yoruba phrase>", ...],
              "requestedAction": "<summary of what was requested>"
            }
            """));

        // Hausa scam detection
        templates.Add(T(
            "Scam Detection — Hausa",
            "ha",
            "scam_detection",
            """
            You are a Nigerian telecoms fraud analyst fluent in Hausa.
            Analyse this Hausa transcript for scam patterns. Hausa scammers often invoke religious authority ("in Allah ya so") or government threats.

            TRANSCRIPT (Hausa):
            {{transcript}}

            KNOWN PATTERNS:
            {{patterns}}

            Key Hausa scam markers:
            - "Don Allah" (for God's sake) before suspicious requests
            - "Za a kama ku" (you will be arrested) — intimidation tactic
            - "Kuɗin aiki" — processing fee requests
            - Cryptocurrency/investment promises: "kuɗinku zai ninkatawa"

            Respond with JSON:
            {
              "isScam": true/false,
              "confidence": 0.0-1.0,
              "category": "<fraud category>",
              "reasoning": "<English explanation>",
              "triggerPhrases": ["<Hausa phrase>", ...],
              "requestedAction": "<what was requested>"
            }
            """));

        // Igbo scam detection
        templates.Add(T(
            "Scam Detection — Igbo",
            "ig",
            "scam_detection",
            """
            You are a Nigerian telecoms fraud analyst fluent in Igbo.
            Analyse this Igbo transcript for potential scam activity.

            TRANSCRIPT (Igbo):
            {{transcript}}

            KNOWN PATTERNS:
            {{patterns}}

            Igbo-specific scam indicators:
            - "Biko" (please) preceding requests for sensitive information
            - "Ọ bụ eziokwu" (it is true) to add false credibility
            - "Zipụ" (send) before requests for airtime codes or OTP
            - Money doubling promises: "ego gị ga abawanye ugboro abụọ"

            Respond with JSON:
            {
              "isScam": true/false,
              "confidence": 0.0-1.0,
              "category": "<fraud category>",
              "reasoning": "<English explanation>",
              "triggerPhrases": ["<Igbo phrase>", ...],
              "requestedAction": "<what was requested>"
            }
            """));

        // Warning SMS — English
        templates.Add(T(
            "Scam Warning SMS — English",
            "en",
            "warning_sms",
            """
            Generate a concise warning SMS (max 160 characters) to send to a Nigerian subscriber who just received a suspected scam call.

            SCAM DETAILS:
            - Category: {{category}}
            - Caller claimed: {{callerClaim}}
            - Requested: {{requestedAction}}

            Requirements:
            - Start with "NAIJASHIELD ALERT:"
            - Be clear and urgent without causing panic
            - Tell them NOT to share OTP/PIN/BVN
            - Mention they can report to 08000-SHIELD

            Output only the SMS text, nothing else.
            """));

        // Warning SMS — Pidgin
        templates.Add(T(
            "Scam Warning SMS — Pidgin",
            "pidgin",
            "warning_sms",
            """
            Generate a warning SMS in Nigerian Pidgin English for a subscriber who just received a scam call.
            Max 160 characters.

            SCAM DETAILS:
            - Category: {{category}}
            - Requested: {{requestedAction}}

            Requirements:
            - Start with "NAIJASHIELD ALERT:"
            - Use simple Pidgin that any Nigerian will understand
            - Warn them not to share OTP, PIN, or any secret number
            - Tell them to call 08000-SHIELD to report

            Output only the SMS text.
            """));

        // Conversation summary
        templates.Add(T(
            "Conversation Summary",
            "en",
            "summarise_conversation",
            """
            Summarise the following customer service conversation in 2-3 sentences.
            Focus on: the customer's main issue, actions taken, and resolution status.

            CONVERSATION:
            {{conversation}}

            Output a concise summary paragraph only. Do not include bullet points or headers.
            """));

        return templates;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetId<T>(T entity, Guid id) where T : Domain.Common.Entity<Guid>
    {
        entity.Id = id;
    }
}
