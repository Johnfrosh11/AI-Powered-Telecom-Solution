using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.Reports;

namespace NaijaShield.Infrastructure.Persistence.Configurations;

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

public class ModelDeploymentConfiguration : IEntityTypeConfiguration<ModelDeployment>
{
    public void Configure(EntityTypeBuilder<ModelDeployment> b)
    {
        b.ToTable("ModelDeployments");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TenantId);
        b.Property(e => e.Precision).HasPrecision(5, 4);
        b.Property(e => e.Recall).HasPrecision(5, 4);
        b.Property(e => e.F1Score).HasPrecision(5, 4);
    }
}
