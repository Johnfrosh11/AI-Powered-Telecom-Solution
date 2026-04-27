using Hangfire;
using NaijaShield.Application;
using NaijaShield.Infrastructure;
using NaijaShield.BackgroundJobs.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHangfire(cfg =>
    cfg.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer(opts =>
{
    opts.WorkerCount = 5;
    opts.Queues = ["default", "critical"];
});

var host = builder.Build();

// ── Register recurring jobs ───────────────────────────────────────────────────

using var scope = host.Services.CreateScope();
var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<OutboxPublisherJob>(
    "outbox-publisher", j => j.ExecuteAsync(CancellationToken.None), "*/30 * * * * *"); // every 30s

recurringJobManager.AddOrUpdate<RefreshTokenCleanerJob>(
    "refresh-token-cleaner", j => j.ExecuteAsync(CancellationToken.None), Cron.Hourly());

recurringJobManager.AddOrUpdate<WatchlistCleanupJob>(
    "watchlist-cleanup", j => j.ExecuteAsync(CancellationToken.None), Cron.Daily(2));

recurringJobManager.AddOrUpdate<ScamPatternRefresherJob>(
    "pattern-refresher", j => j.ExecuteAsync(CancellationToken.None), Cron.Daily(3));

recurringJobManager.AddOrUpdate<DataRetentionPurgerJob>(
    "data-retention-purger", j => j.ExecuteAsync(CancellationToken.None), Cron.Weekly(DayOfWeek.Sunday, 1));

recurringJobManager.AddOrUpdate<CacheWarmerJob>(
    "cache-warmer", j => j.ExecuteAsync(CancellationToken.None), "*/5 * * * *"); // every 5min

await host.RunAsync();

