using ECommerce.SharedKernel.Domain;

namespace ECommerce.Inventory.Domain.Events;

public record LowStockDetectedEvent(
    Guid ProductId,
    int CurrentStock,
    int Threshold
) : DomainEventBase;