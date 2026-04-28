using Microsoft.EntityFrameworkCore;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.Audit;
using NaijaShield.Domain.Aggregates.Conversations;
using NaijaShield.Domain.Aggregates.Customers;
using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Aggregates.Reports;
using NaijaShield.Domain.Aggregates.ScamDetection;
using NaijaShield.Domain.Aggregates.Tenants;

namespace NaijaShield.Infrastructure.Persistence.Repositories;

// ── Base Repository ───────────────────────────────────────────────────────────

internal abstract class BaseRepository<T>(AppDbContext db) where T : Domain.Common.Entity
{
    protected readonly AppDbContext Db = db;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await Db.Set<T>().FindAsync([id], ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct) =>
        await Db.Set<T>().AddAsync(entity, ct);
}

// ── Unit of Work ──────────────────────────────────────────────────────────────

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

// ── Tenant Repository ─────────────────────────────────────────────────────────

internal sealed class TenantRepository(AppDbContext db)
    : BaseRepository<Tenant>(db), ITenantRepository
{
    public Task<Tenant?> GetByNccIdAsync(string nccOperatorId, CancellationToken ct) =>
        Db.Tenants.FirstOrDefaultAsync(t => t.NccOperatorId == nccOperatorId, ct);

    public Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct) =>
        Db.Tenants.ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Tenant>)t.Result);
}

// ── User Repository ───────────────────────────────────────────────────────────

internal sealed class UserRepository(AppDbContext db)
    : BaseRepository<AppUser>(db), IUserRepository
{
    public Task<AppUser?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct) =>
        Db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email &&
                (tenantId == Guid.Empty || u.TenantId == tenantId), ct);

    public Task<AppUser?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct) =>
        Db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

    public async Task<IReadOnlyList<AppUser>> ListAsync(Guid tenantId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = Db.Users.Where(u => u.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
        return await query.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, string? search, CancellationToken ct)
    {
        var query = Db.Users.Where(u => u.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
        return query.CountAsync(ct);
    }
}

// ── Role Repository ───────────────────────────────────────────────────────────

internal sealed class RoleRepository(AppDbContext db)
    : BaseRepository<Role>(db), IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct) =>
        Db.Roles.FirstOrDefaultAsync(r => r.Name == name && r.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Role>> ListAsync(Guid tenantId, CancellationToken ct) =>
        await Db.Roles.Include(r => r.RolePermissions)
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(ct);

    public void Delete(Role role) => Db.Roles.Remove(role);
}

// ── PermissionEntry Repository ────────────────────────────────────────────────

internal sealed class PermissionRepository(AppDbContext db) : IPermissionRepository
{
    public Task<PermissionEntry?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Permissions.FindAsync([id], ct).AsTask();

    public Task<PermissionEntry?> GetByCodeAsync(string code, CancellationToken ct) =>
        db.Permissions.FirstOrDefaultAsync(p => p.Code == code, ct);

    public async Task<IReadOnlyList<PermissionEntry>> ListAllAsync(CancellationToken ct) =>
        await db.Permissions.ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetPermissionCodesForUserAsync(Guid userId, CancellationToken ct)
    {
        var roleIds = await db.Set<UserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        return await db.Set<RolePermissionAssignment>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Code)
            .Distinct()
            .ToListAsync(ct);
    }
}

// ── ApiKey Repository ─────────────────────────────────────────────────────────

internal sealed class ApiKeyRepository(AppDbContext db)
    : BaseRepository<ApiKey>(db), IApiKeyRepository
{
    public Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct) =>
        Db.ApiKeys.FirstOrDefaultAsync(a => a.KeyHash == keyHash, ct);

    public async Task<IReadOnlyList<ApiKey>> ListAsync(Guid tenantId, CancellationToken ct) =>
        await Db.ApiKeys.Where(a => a.TenantId == tenantId).ToListAsync(ct);
}

// ── UserSession Repository ────────────────────────────────────────────────────

internal sealed class UserSessionRepository(AppDbContext db) : IUserSessionRepository
{
    public Task<UserSession?> GetByRefreshTokenHashAsync(string hash, CancellationToken ct) =>
        db.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(Guid userId, CancellationToken ct) =>
        await db.UserSessions
            .Where(s => (userId == Guid.Empty || s.UserId == userId) && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task AddAsync(UserSession session, CancellationToken ct) =>
        await db.UserSessions.AddAsync(session, ct);

    public Task InvalidateUserSessionsAsync(Guid userId, CancellationToken ct) =>
        db.UserSessions.Where(s => s.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);
}

// ── ScamCall Repository ───────────────────────────────────────────────────────

internal sealed class ScamCallRepository(AppDbContext db)
    : BaseRepository<ScamCall>(db), IScamCallRepository
{
    public async Task<IReadOnlyList<ScamCall>> SearchAsync(
        Guid tenantId, string? status, string? language,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var query = Db.ScamCalls.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.ScamCallStatus>(status, out var s))
            query = query.Where(c => c.Status == s);
        if (!string.IsNullOrEmpty(language))
            query = query.Where(c => c.DetectedLanguage == language);
        if (from.HasValue) query = query.Where(c => c.StartedAt >= from.Value);
        if (to.HasValue) query = query.Where(c => c.StartedAt <= to.Value);

        return await query.OrderByDescending(c => c.StartedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, string? status, string? language,
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = Db.ScamCalls.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.ScamCallStatus>(status, out var s))
            query = query.Where(c => c.Status == s);
        if (!string.IsNullOrEmpty(language))
            query = query.Where(c => c.DetectedLanguage == language);
        if (from.HasValue) query = query.Where(c => c.StartedAt >= from.Value);
        if (to.HasValue) query = query.Where(c => c.StartedAt <= to.Value);

        return query.CountAsync(ct);
    }
}

// ── ScamPattern Repository ────────────────────────────────────────────────────

internal sealed class ScamPatternRepository(AppDbContext db)
    : BaseRepository<ScamPattern>(db), IScamPatternRepository
{
    public async Task<IReadOnlyList<ScamPattern>> ListActiveAsync(Guid tenantId, CancellationToken ct) =>
        await Db.ScamPatterns.Include(p => p.Phrases)
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ScamPattern>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct) =>
        await Db.ScamPatterns.Include(p => p.Phrases)
            .Where(p => p.TenantId == tenantId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public void Delete(ScamPattern pattern) => Db.ScamPatterns.Remove(pattern);
}

// ── WatchlistedNumber Repository ──────────────────────────────────────────────

internal sealed class WatchlistedNumberRepository(AppDbContext db)
    : BaseRepository<WatchlistedNumber>(db), IWatchlistedNumberRepository
{
    public Task<WatchlistedNumber?> GetByMsisdnAsync(string msisdn, Guid tenantId, CancellationToken ct)
    {
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(msisdn)));
        return Db.WatchlistedNumbers.FirstOrDefaultAsync(
            n => n.TenantId == tenantId && n.HashedMsisdn == hash, ct);
    }

    public async Task<IReadOnlyList<WatchlistedNumber>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct) =>
        await Db.WatchlistedNumbers.Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.FirstSeen)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct) =>
        Db.WatchlistedNumbers.CountAsync(n => n.TenantId == tenantId, ct);
}

// ── Customer Repository ───────────────────────────────────────────────────────

internal sealed class CustomerRepository(AppDbContext db)
    : BaseRepository<Customer>(db), ICustomerRepository
{
    public Task<Customer?> GetByMsisdnAsync(string msisdn, Guid tenantId, CancellationToken ct)
    {
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(msisdn)));
        return Db.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.HashedMsisdn == hash, ct);
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(Guid tenantId, string query, int page, int pageSize, CancellationToken ct) =>
        await Db.Customers.Where(c => c.TenantId == tenantId &&
            c.MaskedMsisdn.Contains(query))
            .OrderByDescending(c => c.FirstSeen)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<int> SearchCountAsync(Guid tenantId, string query, CancellationToken ct) =>
        Db.Customers.CountAsync(c => c.TenantId == tenantId && c.MaskedMsisdn.Contains(query), ct);

    public async Task<IReadOnlyList<Customer>> ListAsync(Guid tenantId, string? query, int page, int pageSize, CancellationToken ct)
    {
        var q = Db.Customers.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrEmpty(query))
            q = q.Where(c => c.MaskedMsisdn.Contains(query));
        return await q.OrderByDescending(c => c.FirstSeen)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, string? query, CancellationToken ct)
    {
        var q = Db.Customers.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrEmpty(query))
            q = q.Where(c => c.MaskedMsisdn.Contains(query));
        return q.CountAsync(ct);
    }
}

// ── Conversation Repository ───────────────────────────────────────────────────

internal sealed class ConversationRepository(AppDbContext db)
    : BaseRepository<Conversation>(db), IConversationRepository
{
    public override Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Db.Conversations.Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Conversation>> ListAsync(Guid tenantId, string? channel, string? language,
        string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = Db.Conversations.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrEmpty(channel) && Enum.TryParse<Domain.Enums.Channel>(channel, out var ch))
            query = query.Where(c => c.Channel == ch);
        if (!string.IsNullOrEmpty(language))
            query = query.Where(c => c.Language == language);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.ConversationStatus>(status, out var s))
            query = query.Where(c => c.Status == s);
        return await query.OrderByDescending(c => c.LastMessageAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, string? channel, string? language,
        string? status, CancellationToken ct)
    {
        var query = Db.Conversations.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrEmpty(channel) && Enum.TryParse<Domain.Enums.Channel>(channel, out var ch))
            query = query.Where(c => c.Channel == ch);
        if (!string.IsNullOrEmpty(language))
            query = query.Where(c => c.Language == language);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.ConversationStatus>(status, out var s))
            query = query.Where(c => c.Status == s);
        return query.CountAsync(ct);
    }
}

// ── RegulatoryReport Repository ───────────────────────────────────────────────

internal sealed class RegulatoryReportRepository(AppDbContext db)
    : BaseRepository<RegulatoryReport>(db), IRegulatoryReportRepository
{
    public async Task<IReadOnlyList<RegulatoryReport>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct) =>
        await Db.RegulatoryReports.Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.GeneratedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct) =>
        Db.RegulatoryReports.CountAsync(r => r.TenantId == tenantId, ct);
}

// ── PromptTemplate Repository ─────────────────────────────────────────────────

internal sealed class PromptTemplateRepository(AppDbContext db)
    : BaseRepository<PromptTemplate>(db), IPromptTemplateRepository
{
    public async Task<IReadOnlyList<PromptTemplate>> ListAsync(Guid tenantId, int page, int pageSize, CancellationToken ct) =>
        await Db.PromptTemplates.Where(p => p.TenantId == tenantId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<PromptTemplate?> GetActiveByUseCaseAndLanguageAsync(string useCase, string language, Guid tenantId, CancellationToken ct) =>
        Db.PromptTemplates.FirstOrDefaultAsync(p =>
            p.TenantId == tenantId && p.UseCase == useCase && p.Language == language && p.IsActive, ct);
}

// ── AuditLog Repository ───────────────────────────────────────────────────────

internal sealed class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct) =>
        await db.AuditLogs.AddAsync(log, ct);

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.AuditLogs.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AuditLog>> QueryAsync(
        Guid tenantId, Guid? actorId, string? action,
        DateTime? from, DateTime? to, string? sensitivity,
        int page, int pageSize, CancellationToken ct)
    {
        var query = db.AuditLogs.Where(a => a.TenantId == tenantId);
        if (actorId.HasValue) query = query.Where(a => a.UserId == actorId.Value);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
        if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.OccurredAt <= to.Value);
        if (!string.IsNullOrEmpty(sensitivity) && Enum.TryParse<Domain.Enums.AuditSensitivity>(sensitivity, out var sens))
            query = query.Where(a => a.Sensitivity == sens);
        return await query.OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> ListAsync(
        Guid tenantId, Guid? actorId, string? action,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var query = db.AuditLogs.Where(a => a.TenantId == tenantId);
        if (actorId.HasValue) query = query.Where(a => a.UserId == actorId.Value);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
        if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.OccurredAt <= to.Value);
        return await query.OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, Guid? actorId, string? action,
        DateTime? from, DateTime? to, string? sensitivity, CancellationToken ct)
    {
        var query = db.AuditLogs.Where(a => a.TenantId == tenantId);
        if (actorId.HasValue) query = query.Where(a => a.UserId == actorId.Value);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
        if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.OccurredAt <= to.Value);
        if (!string.IsNullOrEmpty(sensitivity) && Enum.TryParse<Domain.Enums.AuditSensitivity>(sensitivity, out var sens))
            query = query.Where(a => a.Sensitivity == sens);
        return query.CountAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct) =>
        db.AuditLogs.CountAsync(a => a.TenantId == tenantId, ct);

    public Task<AuditLog?> GetLatestAsync(Guid tenantId, CancellationToken ct) =>
        db.AuditLogs.Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.OccurredAt).FirstOrDefaultAsync(ct);
}

// ── Outbox Repository ─────────────────────────────────────────────────────────

internal sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken ct) =>
        await db.OutboxMessages.AddAsync(message, ct);

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct) =>
        await db.OutboxMessages.Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt).Take(batchSize).ToListAsync(ct);

    public Task MarkProcessedAsync(Guid id, CancellationToken ct) =>
        db.OutboxMessages.Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ProcessedAt, DateTime.UtcNow), ct);

    public Task MarkFailedAsync(Guid id, string error, CancellationToken ct) =>
        db.OutboxMessages.Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Error, error)
                .SetProperty(m => m.RetryCount, m => m.RetryCount + 1), ct);
}
