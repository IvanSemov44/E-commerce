using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Events;

public record AddressDeletedEvent(
    Guid UserId,
    Guid AddressId,
    string Street,
    string City,
    string Country,
    string? PostalCode) : DomainEventBase;
