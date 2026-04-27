using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.Audit;
using NaijaShield.Domain.Aggregates.Conversations;
using NaijaShield.Domain.Aggregates.Customers;
using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Aggregates.Reports;
using NaijaShield.Domain.Aggregates.ScamDetection;
using NaijaShield.Domain.Aggregates.Tenants;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Infrastructure.Persistence.Configurations;

// ── Tenant ────────────────────────────────────────────────────────────────────

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Slug).IsUnique();
        b.Property(e => e.Plan).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.BlockingConfidenceThreshold).HasPrecision(5, 4);
    }
}

// ── AppUser ───────────────────────────────────────────────────────────────────

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(e => e.Id);
        b.Property(e => e.Email).HasMaxLength(320).IsRequired();
        b.HasIndex(e => e.Email).IsUnique();
        b.Property(e => e.FullName).HasMaxLength(200);
        b.Property(e => e.PasswordHash).HasMaxLength(512);
        b.HasIndex(e => e.EntraObjectId);

        b.HasMany(e => e.UserRoles).WithOne()
            .HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("UserRoles");
        b.HasKey("UserId", "RoleId");
        b.Property<Guid>("UserId");
        b.Property<Guid>("RoleId");
    }
}

// ── Role ──────────────────────────────────────────────────────────────────────

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();

        b.HasMany(e => e.RolePermissions).WithOne()
            .HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<PermissionEntry>
{
    public void Configure(EntityTypeBuilder<PermissionEntry> b)
    {
        b.ToTable("Permissions");
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Code).IsUnique();
    }
}

// ── ScamCall ──────────────────────────────────────────────────────────────────

public class ScamCallConfiguration : IEntityTypeConfiguration<ScamCall>
{
    public void Configure(EntityTypeBuilder<ScamCall> b)
    {
        b.ToTable("ScamCalls");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.CallerMsisdn);
        b.HasIndex(e => new { e.TenantId, e.StartedAt });
        b.Property(e => e.CallerMsisdn).HasMaxLength(20).IsRequired();
        b.Property(e => e.ReceiverMsisdn).HasMaxLength(20).IsRequired();
        b.Property(e => e.DetectedLanguage).HasMaxLength(10).IsRequired();
        b.Property(e => e.AiConfidenceScore).HasPrecision(5, 4);
        b.Property(e => e.EstimatedMoneySaved).HasPrecision(18, 2);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.AiReasoning).HasMaxLength(2000);
    }
}

// ── ScamPattern ───────────────────────────────────────────────────────────────

public class ScamPatternConfiguration : IEntityTypeConfiguration<ScamPattern>
{
    public void Configure(EntityTypeBuilder<ScamPattern> b)
    {
        b.ToTable("ScamPatterns");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.Severity).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.DetectionAccuracy).HasPrecision(5, 4);

        b.HasMany(e => e.Phrases).WithOne()
            .HasForeignKey("PatternId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class ScamPatternPhraseConfiguration : IEntityTypeConfiguration<ScamPatternPhrase>
{
    public void Configure(EntityTypeBuilder<ScamPatternPhrase> b)
    {
        b.ToTable("ScamPatternPhrases");
        b.HasKey(e => e.Id);
        b.Property(e => e.Language).HasMaxLength(10);
        b.Property(e => e.Phrase).HasMaxLength(500);
        b.Property(e => e.Weight).HasPrecision(5, 4);
    }
}

// ── WatchlistedNumber ─────────────────────────────────────────────────────────

public class WatchlistedNumberConfiguration : IEntityTypeConfiguration<WatchlistedNumber>
{
    public void Configure(EntityTypeBuilder<WatchlistedNumber> b)
    {
        b.ToTable("WatchlistedNumbers");
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.TenantId, e.HashedMsisdn }).IsUnique();
        b.Property(e => e.HashedMsisdn).HasMaxLength(256).IsRequired();
        b.Property(e => e.MaskedNumber).HasMaxLength(20).IsRequired();
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
    }
}

// ── ScamWarning ───────────────────────────────────────────────────────────────

public class ScamWarningConfiguration : IEntityTypeConfiguration<ScamWarning>
{
    public void Configure(EntityTypeBuilder<ScamWarning> b)
    {
        b.ToTable("ScamWarnings");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
    }
}

// ── Customer ──────────────────────────────────────────────────────────────────

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.TenantId, e.HashedMsisdn }).IsUnique();
        b.Property(e => e.HashedMsisdn).HasMaxLength(256).IsRequired();
        b.Property(e => e.MaskedMsisdn).HasMaxLength(20).IsRequired();
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.FraudRiskScore).HasPrecision(5, 4);
    }
}

// ── Conversation / Message ────────────────────────────────────────────────────

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("Conversations");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.CustomerId);
        b.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Language).HasMaxLength(10);
        b.Property(e => e.Summary).HasMaxLength(2000);
        b.Property(e => e.CustomerMsisdn).HasMaxLength(20);

        b.HasMany(e => e.Messages).WithOne()
            .HasForeignKey("ConversationId").OnDelete(DeleteBehavior.Cascade);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("Messages");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.SentAt);
        b.Property(e => e.Content).HasMaxLength(4000);
        b.Property(e => e.ContentEnglish).HasMaxLength(4000);
        b.Property(e => e.MessageType).HasConversion<string>().HasMaxLength(50);
    }
}

// ── RegulatoryReport ──────────────────────────────────────────────────────────

public class RegulatoryReportConfiguration : IEntityTypeConfiguration<RegulatoryReport>
{
    public void Configure(EntityTypeBuilder<RegulatoryReport> b)
    {
        b.ToTable("RegulatoryReports");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.OutputBlobUri).HasMaxLength(1000);
    }
}

// ── PromptTemplate ────────────────────────────────────────────────────────────

public class PromptTemplateConfiguration : IEntityTypeConfiguration<PromptTemplate>
{
    public void Configure(EntityTypeBuilder<PromptTemplate> b)
    {
        b.ToTable("PromptTemplates");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.Language).HasMaxLength(10);
        b.Property(e => e.UseCase).HasMaxLength(100);
        b.Property(e => e.SuccessRate).HasPrecision(5, 4);
    }
}

// ── AuditLog ──────────────────────────────────────────────────────────────────

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.UserId);
        b.HasIndex(e => e.OccurredAt);
        b.Property(e => e.OccurredAt).IsRequired();
        b.Property(e => e.Action).HasMaxLength(200).IsRequired();
        b.Property(e => e.TargetType).HasMaxLength(100);
        b.Property(e => e.IpAddress).HasMaxLength(45);
        b.Property(e => e.UserAgent).HasMaxLength(500);
        b.Property(e => e.ChainHash).HasMaxLength(256);
        b.Property(e => e.Result).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Sensitivity).HasConversion<string>().HasMaxLength(50);
    }
}

// ── OutboxMessage ─────────────────────────────────────────────────────────────

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.ProcessedAt);
        b.Property(e => e.Type).HasMaxLength(200).IsRequired();
        b.Property(e => e.Payload).HasMaxLength(8000).IsRequired();
    }
}

// ── FeatureFlag ───────────────────────────────────────────────────────────────

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> b)
    {
        b.ToTable("FeatureFlags");
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
        b.Property(e => e.Key).HasMaxLength(100).IsRequired();
    }
}
