using ECommerce.SharedKernel.Domain;

namespace ECommerce.Reviews.Domain.Events;

public sealed record ReviewRatingProjectionChangedDomainEvent(Guid ProductId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
