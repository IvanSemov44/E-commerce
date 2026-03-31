using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Events;

public record EmailVerifiedEvent(Guid UserId) : DomainEventBase;
