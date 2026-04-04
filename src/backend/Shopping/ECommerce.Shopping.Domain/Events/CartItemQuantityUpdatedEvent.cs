using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Events;

public record CartItemQuantityUpdatedEvent(
    Guid CartId,
    Guid ProductId,
    int  NewQuantity
) : DomainEventBase;