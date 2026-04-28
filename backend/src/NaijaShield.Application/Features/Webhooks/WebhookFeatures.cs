using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Features.Webhooks;

/// <summary>
/// Webhook registration stored as a simple configuration record.
/// Since a full Webhook domain entity doesn't exist yet, we model it through
/// the Integration settings stored in the tenant config.
/// Handlers use stub in-memory state for now; replace with a proper repository
/// once the WebhookEndpoint entity is added to the domain layer.
/// </summary>

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record WebhookDto(Guid Id, Guid TenantId, string Url, string[] Events, bool IsActive, DateTime CreatedAt);

// ── List Webhooks ─────────────────────────────────────────────────────────────

public record ListWebhooksQuery(Guid TenantId)
    : IRequest<Result<IReadOnlyList<WebhookDto>>>;

public class ListWebhooksQueryHandler()
    : IRequestHandler<ListWebhooksQuery, Result<IReadOnlyList<WebhookDto>>>
{
    public Task<Result<IReadOnlyList<WebhookDto>>> Handle(ListWebhooksQuery q, CancellationToken ct)
    {
        // TODO: replace with IWebhookRepository once domain entity is added
        IReadOnlyList<WebhookDto> empty = [];
        return Task.FromResult(Result.Success(empty));
    }
}

// ── Create Webhook ────────────────────────────────────────────────────────────

public record CreateWebhookCommand(Guid TenantId, string Url, string[] Events, string Secret)
    : IRequest<Result<Guid>>, ITransactionalCommand;

public class CreateWebhookCommandHandler()
    : IRequestHandler<CreateWebhookCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateWebhookCommand cmd, CancellationToken ct)
    {
        // TODO: persist once WebhookEndpoint domain entity is wired up
        var id = Guid.NewGuid();
        return Task.FromResult(Result.Success(id));
    }
}

// ── Delete Webhook ────────────────────────────────────────────────────────────

public record DeleteWebhookCommand(Guid WebhookId, Guid TenantId)
    : IRequest<Result>, ITransactionalCommand;

public class DeleteWebhookCommandHandler()
    : IRequestHandler<DeleteWebhookCommand, Result>
{
    public Task<Result> Handle(DeleteWebhookCommand cmd, CancellationToken ct)
    {
        // TODO: delete from IWebhookRepository once entity is available
        return Task.FromResult(Result.Success());
    }
}

// ── Process CDR Webhook ───────────────────────────────────────────────────────

public record ProcessCdrWebhookCommand(
    Guid TenantId, string CallId, string CallerMsisdn, string CalledMsisdn,
    int DurationSeconds, DateTime CallStartedAt, string AudioBlobUrl)
    : IRequest<Result<Guid>>, ITransactionalCommand;

public class ProcessCdrWebhookCommandHandler(IMediator mediator)
    : IRequestHandler<ProcessCdrWebhookCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProcessCdrWebhookCommand cmd, CancellationToken ct)
    {
        // Delegate to IngestCallAudioCommand in the Fraud feature
        using var audioStream = new System.IO.MemoryStream(); // CDR webhook has no audio stream
        var result = await mediator.Send(new Application.Features.Fraud.IngestCallAudioCommand(
            cmd.TenantId, cmd.CallerMsisdn, cmd.CalledMsisdn,
            cmd.CallStartedAt, TimeSpan.FromSeconds(cmd.DurationSeconds),
            audioStream), ct);

        return result;
    }
}
