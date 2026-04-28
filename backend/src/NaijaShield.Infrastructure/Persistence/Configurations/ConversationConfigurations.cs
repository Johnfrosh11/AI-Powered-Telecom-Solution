using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaShield.Domain.Aggregates.Conversations;
using NaijaShield.Domain.Aggregates.Customers;

namespace NaijaShield.Infrastructure.Persistence.Configurations;

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
        b.Property(e => e.ContentOriginal).HasMaxLength(4000);
        b.Property(e => e.ContentEnglish).HasMaxLength(4000);
        b.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
        b.Ignore(e => e.Content);
        b.Ignore(e => e.MessageType);
        b.Ignore(e => e.IsFromCustomer);
    }
}
