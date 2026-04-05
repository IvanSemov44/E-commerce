using ECommerce.SharedKernel.Domain;

namespace ECommerce.Ordering.Domain.Events;

public sealed record OrderConfirmedEvent(Guid OrderId, Guid UserId) : DomainEventBase;

public sealed record OrderShippedEvent(Guid OrderId, Guid UserId, string TrackingNumber) : DomainEventBase;

public sealed record OrderDeliveredEvent(Guid OrderId, Guid UserId, IReadOnlyList<Guid> ProductIds) : DomainEventBase;

public sealed record OrderCancelledEvent(Guid OrderId, Guid UserId, string Reason) : DomainEventBase;
