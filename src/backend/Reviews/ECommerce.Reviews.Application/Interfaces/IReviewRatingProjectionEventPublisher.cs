namespace ECommerce.Reviews.Application.Interfaces;

public interface IReviewRatingProjectionEventPublisher
{
    Task PublishProductRatingProjectionUpdatedAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
