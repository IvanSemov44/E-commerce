using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Reviews.Application.Interfaces;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Infrastructure.Persistence;

namespace ECommerce.Reviews.Infrastructure.Services;

/// <summary>
/// Enqueues rating projection integration events directly into the Reviews-owned
/// outbox table (reviews.outbox_messages). This makes the enqueue atomic with the
/// aggregate save — both happen inside the same ReviewsDbContext connection.
/// </summary>
public sealed class ReviewRatingProjectionEventPublisher(
    IReviewRepository reviewRepository,
    ReviewsDbContext dbContext) : IReviewRatingProjectionEventPublisher
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

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

        var evt = new ProductRatingProjectionUpdatedIntegrationEvent(
            productId,
            averageRating,
            totalApprovedReviews,
            DateTime.UtcNow);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = evt.IdempotencyKey,
            EventType = evt.GetType().AssemblyQualifiedName ?? evt.GetType().FullName!,
            EventData = JsonSerializer.Serialize(evt, evt.GetType(), _json),
            CreatedAt = DateTime.UtcNow
        });
    }
}
