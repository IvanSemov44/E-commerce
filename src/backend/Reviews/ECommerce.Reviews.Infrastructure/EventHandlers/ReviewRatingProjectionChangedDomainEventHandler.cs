using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Domain.Events;
using MediatR;

namespace ECommerce.Reviews.Infrastructure.EventHandlers;

public sealed class ReviewRatingProjectionChangedDomainEventHandler(
    IReviewRatingProjectionEventPublisher ratingProjectionEventPublisher)
    : INotificationHandler<ReviewRatingProjectionChangedDomainEvent>
{
    public Task Handle(ReviewRatingProjectionChangedDomainEvent notification, CancellationToken cancellationToken)
        => ratingProjectionEventPublisher.PublishProductRatingProjectionUpdatedAsync(
            notification.ProductId,
            cancellationToken);
}
