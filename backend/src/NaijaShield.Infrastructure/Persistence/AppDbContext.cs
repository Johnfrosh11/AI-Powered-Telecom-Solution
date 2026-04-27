using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.Audit;
using NaijaShield.Domain.Aggregates.Conversations;
using NaijaShield.Domain.Aggregates.Customers;
using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Aggregates.Reports;
using NaijaShield.Domain.Aggregates.ScamDetection;
using NaijaShield.Domain.Aggregates.Tenants;

namespace NaijaShield.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    // Identity
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<PermissionEntry> Permissions => Set<PermissionEntry>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Scam Detection
    public DbSet<ScamCall> ScamCalls => Set<ScamCall>();
    public DbSet<ScamPattern> ScamPatterns => Set<ScamPattern>();
    public DbSet<WatchlistedNumber> WatchlistedNumbers => Set<WatchlistedNumber>();
    public DbSet<ScamWarning> ScamWarnings => Set<ScamWarning>();

    // Customers / Conversations
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Conversation> Conversations => Set<Conversation>();

    // Reports
    public DbSet<RegulatoryReport> RegulatoryReports => Set<RegulatoryReport>();
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();

    // AI Studio
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<SemanticKernelSkill> SemanticKernelSkills => Set<SemanticKernelSkill>();
    public DbSet<ModelDeployment> ModelDeployments => Set<ModelDeployment>();

    // Audit / Infra
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<DataRetentionPolicy> DataRetentionPolicies => Set<DataRetentionPolicy>();
    public DbSet<DataExportRequest> DataExportRequests => Set<DataExportRequest>();
    public DbSet<Integration> Integrations => Set<Integration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete filter (except audit logs — never soft-deleted)
        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Domain.Common.Entity).IsAssignableFrom(type.ClrType) &&
                type.ClrType != typeof(AuditLog))
            {
                modelBuilder.Entity(type.ClrType)
                    .HasQueryFilter(BuildIsDeletedFilter(type.ClrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression BuildIsDeletedFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var prop = System.Linq.Expressions.Expression.Property(param, "IsDeleted");
        var notDeleted = System.Linq.Expressions.Expression.Not(prop);
        return System.Linq.Expressions.Expression.Lambda(notDeleted, param);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Domain.Common.Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = now;
                entry.Property("UpdatedAt").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
