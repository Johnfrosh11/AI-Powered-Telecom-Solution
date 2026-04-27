using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Aggregates.ScamDetection;
using NaijaShield.Domain.Aggregates.Customers;
using NaijaShield.Domain.Aggregates.Conversations;
using NaijaShield.Domain.Aggregates.Reports;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.Audit;
using NaijaShield.Domain.Aggregates.Tenants;

namespace NaijaShield.Application.Common.Interfaces;

/// <summary>Unit of Work — coordinates a transaction across all repositories.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetByNccIdAsync(string nccOperatorId, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task<AppUser?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> ListAsync(Guid tenantId, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, string? search, CancellationToken ct = default);
}

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    void Delete(Role role);
}

public interface IPermissionRepository
{
    Task<PermissionEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PermissionEntry?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<PermissionEntry>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetPermissionCodesForUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(ApiKey key, CancellationToken ct = default);
}

public interface IUserSessionRepository
{
    Task<UserSession?> GetByRefreshTokenHashAsync(string hash, CancellationToken ct = default);
    Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserSession session, CancellationToken ct = default);
    Task InvalidateUserSessionsAsync(Guid userId, CancellationToken ct = default);
}

public interface IScamCallRepository
{
    Task<ScamCall?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ScamCall call, CancellationToken ct = default);
    Task<IReadOnlyList<ScamCall>> SearchAsync(
        Guid tenantId, string? status, string? language,
        DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, string? status, string? language, DateTime? from, DateTime? to, CancellationToken ct = default);
}

public interface IScamPatternRepository
{
    Task<ScamPattern?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ScamPattern>> ListActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ScamPattern>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(ScamPattern pattern, CancellationToken ct = default);
    void Delete(ScamPattern pattern);
}

public interface IWatchlistedNumberRepository
{
    Task<WatchlistedNumber?> GetByMsisdnAsync(string msisdn, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<WatchlistedNumber>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(WatchlistedNumber number, CancellationToken ct = default);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> GetByMsisdnAsync(string msisdn, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> SearchAsync(Guid tenantId, string query, int page, int pageSize, CancellationToken ct = default);
    Task<int> SearchCountAsync(Guid tenantId, string query, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> ListAsync(Guid tenantId, string? region, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, string? status, CancellationToken ct = default);
}

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> ListAsync(Guid tenantId, string? channel, string? language, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, string? channel, string? language, string? status, CancellationToken ct = default);
}

public interface IRegulatoryReportRepository
{
    Task<RegulatoryReport?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RegulatoryReport report, CancellationToken ct = default);
    Task<IReadOnlyList<RegulatoryReport>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
}

public interface IPromptTemplateRepository
{
    Task<PromptTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PromptTemplate?> GetActiveByUseCaseAndLanguageAsync(string useCase, string language, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<PromptTemplate>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(PromptTemplate template, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> QueryAsync(
        Guid tenantId, Guid? actorId, string? action,
        DateTime? from, DateTime? to, string? sensitivity,
        int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> ListAsync(
        Guid tenantId, Guid? actorId, string? action,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, Guid? actorId, string? action, DateTime? from, DateTime? to, string? sensitivity, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
    Task<AuditLog?> GetLatestAsync(Guid tenantId, CancellationToken ct = default);
}

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
}
