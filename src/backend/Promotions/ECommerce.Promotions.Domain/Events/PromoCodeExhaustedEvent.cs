using ECommerce.SharedKernel.Domain;

namespace ECommerce.Promotions.Domain.Events;

public record PromoCodeExhaustedEvent(Guid PromoCodeId, string Code) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}