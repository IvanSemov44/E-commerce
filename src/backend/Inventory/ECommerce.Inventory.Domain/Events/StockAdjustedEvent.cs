using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Events;

public record StockAdjustedEvent(
    Guid ProductId,
    int NewQuantity,
    string Reason
) : DomainEventBase;
