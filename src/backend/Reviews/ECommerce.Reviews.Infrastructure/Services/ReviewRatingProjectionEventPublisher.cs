using ECommerce.Contracts;
using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Domain.Interfaces;

namespace ECommerce.Reviews.Infrastructure.Services;

public sealed class ReviewRatingProjectionEventPublisher(
    IReviewRepository reviewRepository,
    IIntegrationEventOutbox outbox) : IReviewRatingProjectionEventPublisher
{
    public async Task PublishProductRatingProjectionUpdatedAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var averageRating = await reviewRepository.GetAverageRatingAsync(productId, cancellationToken);
        var (_, totalApprovedReviews) = await reviewRepository.GetByProductAsync(
            productId,
            page: 1,
            pageSize: 1,
            onlyApproved: true,
            cancellationToken: cancellationToken);

        var integrationEvent = new ProductRatingProjectionUpdatedIntegrationEvent(
            productId,
            averageRating,
            totalApprovedReviews,
            DateTime.UtcNow);

        await outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
