using ECommerce.Contracts;
using Microsoft.Extensions.Logging;

namespace ECommerce.Reviews.Application.EventHandlers;

public class MarkReviewVerifiedOnOrderDeliveredHandler(
    IReviewRepository reviews,
    IUnitOfWork uow,
    ILogger<MarkReviewVerifiedOnOrderDeliveredHandler> logger) : INotificationHandler<OrderDeliveredIntegrationEvent>
{

    public async Task Handle(OrderDeliveredIntegrationEvent notification, CancellationToken ct)
    {
        try
        {
            foreach (var productId in notification.ProductIds)
            {
                var (items, _) = await reviews.GetByProductAsync(productId, 1, 100, false, ct);
                foreach (var review in items.Where(r => r.UserId == notification.UserId && !r.IsVerifiedPurchase))
                {
                    review.MarkAsVerifiedPurchase();
                }
            }
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Marked reviews as verified for user {UserId} order {OrderId}", notification.UserId, notification.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark reviews as verified for order {OrderId}", notification.OrderId);
        }
    }
}
