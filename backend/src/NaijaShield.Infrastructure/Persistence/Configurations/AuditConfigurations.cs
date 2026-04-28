using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaShield.Domain.Aggregates.Audit;

namespace NaijaShield.Infrastructure.Persistence.Configurations;

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

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.ProcessedAt);
        b.HasIndex(e => new { e.Type, e.ProcessedAt });
        b.Property(e => e.Type).HasMaxLength(200).IsRequired();
        b.Property(e => e.Payload).HasMaxLength(8000).IsRequired();
    }
}

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> b)
    {
        b.ToTable("FeatureFlags");
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
        b.Property(e => e.Key).HasMaxLength(100).IsRequired();
        b.Property(e => e.RolloutPercentage).HasPrecision(5, 2);
    }
}
