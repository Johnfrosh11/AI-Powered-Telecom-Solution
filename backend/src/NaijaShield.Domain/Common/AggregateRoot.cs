using MediatR;

namespace NaijaShield.Domain.Common;

/// <summary>Aggregate root that can raise domain events.</summary>
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<INotification> _domainEvents = [];

    /// <summary>Read-only view of pending domain events.</summary>
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Enqueue a domain event for dispatch.</summary>
    protected void RaiseDomainEvent(INotification domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>Clear all pending domain events (called after dispatch).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
