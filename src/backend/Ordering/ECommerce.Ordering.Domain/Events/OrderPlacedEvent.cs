using ECommerce.SharedKernel.Domain;
using ECommerce.Ordering.Domain.Aggregates.Order;

namespace ECommerce.Ordering.Domain.Events;

public sealed record OrderPlacedEvent(Guid OrderId, Guid UserId, decimal Total, IReadOnlyList<OrderItemData> Items) : DomainEventBase;
