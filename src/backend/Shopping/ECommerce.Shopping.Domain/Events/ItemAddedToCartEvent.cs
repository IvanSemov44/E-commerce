using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Events;

public record ItemAddedToCartEvent(
    Guid CartId,
    Guid ProductId,
    int  Quantity
) : DomainEventBase;