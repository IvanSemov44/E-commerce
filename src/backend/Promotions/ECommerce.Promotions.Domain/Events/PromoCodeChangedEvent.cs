using ECommerce.SharedKernel.Domain;

namespace ECommerce.Promotions.Domain.Events;

/// <summary>
/// Raised whenever a promo code is created, updated, deactivated, or soft-deleted.
/// Consumed by Infrastructure to push a projection-update integration event to the outbox.
/// </summary>
public record PromoCodeChangedEvent(
    Guid PromoCodeId,
    string Code,
    decimal DiscountValue,
    bool IsActive,
    bool IsDeleted) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
