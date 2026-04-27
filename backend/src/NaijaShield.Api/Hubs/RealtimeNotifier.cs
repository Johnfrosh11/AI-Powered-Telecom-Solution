using Microsoft.AspNetCore.SignalR;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Api.Hubs;

internal sealed class RealtimeNotifier(IHubContext<FraudHub> fraudHub) : IRealtimeNotifier
{
    public Task NotifyTenantAsync(Guid tenantId, string eventName, object payload, CancellationToken ct) =>
        fraudHub.Clients.Group(tenantId.ToString()).SendAsync(eventName, payload, ct);

    public Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken ct) =>
        fraudHub.Clients.User(userId.ToString()).SendAsync(eventName, payload, ct);
}
