using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Events;

public record StockReplenishedEvent(
    Guid ProductId,
    int QuantityAdded,
    int NewQuantity
) : DomainEventBase;