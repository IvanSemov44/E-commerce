using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : DomainEventBase;
