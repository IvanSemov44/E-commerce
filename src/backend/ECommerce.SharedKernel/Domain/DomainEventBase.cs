namespace ECommerce.SharedKernel.Domain;

public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
