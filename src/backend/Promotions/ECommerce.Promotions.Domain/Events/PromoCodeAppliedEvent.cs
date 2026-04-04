using ECommerce.SharedKernel.Domain;

namespace ECommerce.Promotions.Domain.Events;

public record PromoCodeAppliedEvent(Guid PromoCodeId, string Code) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}