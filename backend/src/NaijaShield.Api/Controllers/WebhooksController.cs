using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Webhooks;
using NaijaShield.Domain.Constants;
using System.Security.Cryptography;
using System.Text;

namespace NaijaShield.Api.Controllers;

[Authorize]
public class WebhooksController(IMediator mediator, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        if (!currentUser.HasPermission(Permissions.WebhooksManage)) return Forbid();
        return HandleResult(await mediator.Send(
            new ListWebhooksQuery(currentUser.TenantId ?? Guid.Empty), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookCommand cmd, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.WebhooksManage)) return Forbid();
        var command = cmd with { TenantId = currentUser.TenantId ?? Guid.Empty };
        return HandleResult(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permissions.WebhooksManage)) return Forbid();
        return HandleResult(await mediator.Send(
            new DeleteWebhookCommand(id, currentUser.TenantId ?? Guid.Empty), ct));
    }

    /// <summary>Inbound webhook from telecom CDR providers (unsigned public endpoint).</summary>
    [HttpPost("inbound/cdr")]
    [AllowAnonymous]
    public async Task<IActionResult> InboundCdr(
        [FromBody] CdrWebhookPayload payload,
        [FromHeader(Name = "X-Webhook-Signature")] string? signature,
        CancellationToken ct = default)
    {
        // Signature validation — reject forged payloads
        if (!WebhookSignatureValidator.Verify(
                System.Text.Json.JsonSerializer.Serialize(payload),
                signature ?? string.Empty,
                Environment.GetEnvironmentVariable("WEBHOOK_SECRET") ?? string.Empty))
        {
            return Unauthorized(new { error = "Invalid webhook signature." });
        }

        return HandleResult(await mediator.Send(
            new ProcessCdrWebhookCommand(payload.TenantId, Guid.NewGuid().ToString(),
                payload.CallerMsisdn, payload.ReceiverMsisdn,
                payload.DurationSeconds, payload.StartedAt, string.Empty), ct));
    }
}

public record CdrWebhookPayload(
    Guid TenantId, string CallerMsisdn, string ReceiverMsisdn,
    DateTime StartedAt, int DurationSeconds);

/// <summary>Verifies HMAC-SHA256 signatures on inbound webhook payloads.</summary>
public static class WebhookSignatureValidator
{
    public static bool Verify(string payload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signature))
            return false;

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var expectedBytes = HMACSHA256.HashData(keyBytes, payloadBytes);
        var expected = $"sha256={Convert.ToHexString(expectedBytes).ToLowerInvariant()}";

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(expected));
    }
}
