using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Events;

public record CartClearedEvent(
    Guid CartId,
    Guid UserId
) : DomainEventBase;