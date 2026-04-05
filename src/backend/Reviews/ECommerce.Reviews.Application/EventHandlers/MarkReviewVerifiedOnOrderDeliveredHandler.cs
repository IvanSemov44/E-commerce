using MediatR;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Ordering.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Reviews.Application.EventHandlers;

public class MarkReviewVerifiedOnOrderDeliveredHandler : INotificationHandler<OrderDeliveredEvent>
{
    private readonly IReviewRepository _reviews;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<MarkReviewVerifiedOnOrderDeliveredHandler> _logger;

    public MarkReviewVerifiedOnOrderDeliveredHandler(
        IReviewRepository reviews,
        IUnitOfWork uow,
        ILogger<MarkReviewVerifiedOnOrderDeliveredHandler> logger)
    {
        _reviews = reviews;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(OrderDeliveredEvent notification, CancellationToken ct)
    {
        try
        {
            foreach (var productId in notification.ProductIds)
            {
                var (items, _) = await _reviews.GetByProductAsync(productId, 1, 100, false, ct);
                foreach (var review in items.Where(r => r.UserId == notification.UserId && !r.IsVerifiedPurchase))
                {
                    review.MarkAsVerifiedPurchase();
                    await _reviews.UpsertAsync(review, ct);
                }
            }
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Marked reviews as verified for user {UserId} order {OrderId}", notification.UserId, notification.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark reviews as verified for order {OrderId}", notification.OrderId);
        }
    }
}
