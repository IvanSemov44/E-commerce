using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Events;

public record PasswordChangedEvent(Guid UserId) : DomainEventBase;
