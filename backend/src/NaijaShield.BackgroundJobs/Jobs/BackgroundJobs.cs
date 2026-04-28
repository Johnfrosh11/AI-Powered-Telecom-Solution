using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Fraud;
using NaijaShield.Domain.Constants;
using NaijaShield.Infrastructure.Persistence;

namespace NaijaShield.BackgroundJobs.Jobs;

// ── Outbox Publisher ──────────────────────────────────────────────────────────

public class OutboxPublisherJob(
    AppDbContext db,
    IAzureServiceBus serviceBus,
    ILogger<OutboxPublisherJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(60)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            try
            {
                await serviceBus.PublishAsync(msg.Type, msg.Payload, ct);
                msg.ProcessedAt = DateTime.UtcNow;
                logger.LogInformation("Outbox message {Id} published to topic {Type}", msg.Id, msg.Type);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

// ── Refresh Token Cleaner ─────────────────────────────────────────────────────

public class RefreshTokenCleanerJob(
    IUnitOfWork unitOfWork,
    ILogger<RefreshTokenCleanerJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Expired refresh tokens purged");
    }
}

// ── Watchlist Cleanup ─────────────────────────────────────────────────────────

public class WatchlistCleanupJob(
    AppDbContext db,
    ILogger<WatchlistCleanupJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-180); // 6-month rolling window
        var removed = await db.WatchlistedNumbers
            .Where(n => n.LastSeen < cutoff && n.Status == Domain.Enums.WatchlistStatus.Monitored)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Watchlist cleanup: {Count} stale entries removed", removed);
    }
}

// ── Scam Pattern Refresher ────────────────────────────────────────────────────

public class ScamPatternRefresherJob(
    AppDbContext db,
    ILogger<ScamPatternRefresherJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    [DisableConcurrentExecution(120)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Re-compute detection accuracy for each pattern based on recent calls
        var patterns = await db.ScamPatterns.Where(p => p.IsActive).ToListAsync(ct);

        foreach (var pattern in patterns)
        {
            var matchedCount = await db.ScamCalls
                .CountAsync(c => c.MatchedPatternId == pattern.Id &&
                    c.Status == Domain.Enums.ScamCallStatus.Confirmed &&
                    c.StartedAt >= DateTime.UtcNow.AddDays(-30), ct);

            var totalCount = await db.ScamCalls
                .CountAsync(c => c.MatchedPatternId == pattern.Id &&
                    c.StartedAt >= DateTime.UtcNow.AddDays(-30), ct);

            if (totalCount > 0)
                pattern.UpdateAccuracy((decimal)matchedCount / totalCount);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Pattern accuracy refreshed for {Count} patterns", patterns.Count);
    }
}

// ── Data Retention Purger ─────────────────────────────────────────────────────

public class DataRetentionPurgerJob(
    AppDbContext db,
    ILogger<DataRetentionPurgerJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    [DisableConcurrentExecution(300)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var policies = await db.DataRetentionPolicies.ToListAsync(ct);

        foreach (var policy in policies)
        {
            var cutoff = DateTime.UtcNow.AddDays(-policy.RetentionDays);
            int deleted = 0;

            switch (policy.DataCategory)
            {
                case "ScamCall":
                    deleted = await db.ScamCalls
                        .Where(c => c.TenantId == policy.TenantId && c.CreatedAt < cutoff)
                        .ExecuteDeleteAsync(ct);
                    break;
                case "AuditLog":
                    deleted = await db.AuditLogs
                        .Where(a => a.TenantId == policy.TenantId && a.OccurredAt < cutoff)
                        .ExecuteDeleteAsync(ct);
                    break;
                case "Conversation":
                    deleted = await db.Conversations
                        .Where(c => c.TenantId == policy.TenantId && c.CreatedAt < cutoff)
                        .ExecuteDeleteAsync(ct);
                    break;
            }

            logger.LogInformation(
                "Data retention: purged {Count} {Type} records for tenant {TenantId}",
                deleted, policy.DataCategory, policy.TenantId);
        }
    }
}

// ── CDR Ingestion Worker ──────────────────────────────────────────────────────

/// <summary>
/// Polls Azure Service Bus for unprocessed CDR (Call Detail Record) messages published by
/// telecom switches and feeds them into the AI fraud-detection pipeline.
/// Each message carries caller MSISDN, receiver MSISDN, duration, and a blob URI pointing
/// to the recorded audio file.
/// </summary>
public class CdrIngestionJob(
    AppDbContext db,
    IAzureBlobStorage blobStorage,
    IMediator mediator,
    ILogger<CdrIngestionJob> logger)
{
    private const int BatchSize = 20;

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(120)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Fetch unprocessed CDRs surfaced as OutboxMessages of type "cdr.received"
        var pending = await db.OutboxMessages
            .Where(m => m.Type == "cdr.received" && m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            logger.LogDebug("CDR ingestion: no pending records");
            return;
        }

        logger.LogInformation("CDR ingestion: processing {Count} records", pending.Count);
        var processed = 0;

        foreach (var msg in pending)
        {
            try
            {
                var cdr = System.Text.Json.JsonSerializer.Deserialize<CdrMessage>(msg.Payload);
                if (cdr is null)
                {
                    msg.Error = "Payload deserialization returned null";
                    msg.RetryCount++;
                    continue;
                }

                // Download audio from blob storage
                Stream audioStream;
                try
                {
                    var parts = cdr.AudioBlobUri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var container = parts.Length >= 2 ? parts[^2] : BlobContainers.CallRecordings;
                    var blobName = parts.Length >= 1 ? parts[^1] : cdr.AudioBlobUri;
                    audioStream = await blobStorage.DownloadAsync(container, blobName, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "CDR {Id}: audio download failed — skipping", msg.Id);
                    msg.Error = ex.Message;
                    msg.RetryCount++;
                    continue;
                }

                // Push through the full AI pipeline
                var command = new IngestCallAudioCommand(
                    cdr.TenantId,
                    cdr.CallerMsisdn,
                    cdr.ReceiverMsisdn,
                    cdr.StartedAt,
                    TimeSpan.FromSeconds(cdr.DurationSeconds),
                    audioStream,
                    cdr.SuspectedLanguage ?? "en");

                var result = await mediator.Send(command, ct);

                if (result.IsSuccess)
                {
                    msg.ProcessedAt = DateTime.UtcNow;
                    processed++;
                    logger.LogInformation("CDR {Id}: ingested → ScamCall {ScamCallId}", msg.Id, result.Value);
                }
                else
                {
                    msg.Error = result.Error ?? "Unknown error";
                    msg.RetryCount++;
                    logger.LogWarning("CDR {Id}: pipeline returned failure — {Error}", msg.Id, result.Error);
                }
            }
            catch (Exception ex)
            {
                msg.Error = ex.Message;
                msg.RetryCount++;
                logger.LogError(ex, "CDR {Id}: unexpected error during ingestion", msg.Id);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("CDR ingestion: {Processed}/{Total} records processed", processed, pending.Count);
    }

    /// <summary>Shape of the CDR payload stored in OutboxMessage.Payload.</summary>
    private sealed record CdrMessage(
        Guid TenantId,
        string CallerMsisdn,
        string ReceiverMsisdn,
        DateTime StartedAt,
        int DurationSeconds,
        string AudioBlobUri,
        string? SuspectedLanguage);
}

public class CacheWarmerJob(
    AppDbContext db,
    IPermissionCache permissionCache,
    ILogger<CacheWarmerJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var users = await db.Users
            .Where(u => u.LastLoginAt >= DateTime.UtcNow.AddHours(-1))
            .ToListAsync(ct);

        foreach (var user in users)
        {
            var roleIds = user.UserRoles.Select(r => r.RoleId).ToList();
            var permissions = await db.Roles
                .Where(r => roleIds.Contains(r.Id))
                .SelectMany(r => r.RolePermissions.Select(rp => rp.PermissionEntry != null ? rp.PermissionEntry.Code : string.Empty))
                .Distinct()
                .ToListAsync(ct);

            await permissionCache.SetAsync(user.Id, permissions, ct);
        }

        logger.LogInformation("Cache warmed for {Count} recently active users", users.Count);
    }
}
