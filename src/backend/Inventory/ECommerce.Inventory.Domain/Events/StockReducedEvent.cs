using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Events;

public record StockReducedEvent(
    Guid InventoryItemId,
    Guid ProductId,
    int QuantityReduced,
    int NewQuantity,
    string Reason
) : DomainEventBase;