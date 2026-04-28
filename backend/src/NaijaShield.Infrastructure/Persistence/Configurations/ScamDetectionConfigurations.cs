using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaShield.Domain.Aggregates.ScamDetection;

namespace NaijaShield.Infrastructure.Persistence.Configurations;

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
