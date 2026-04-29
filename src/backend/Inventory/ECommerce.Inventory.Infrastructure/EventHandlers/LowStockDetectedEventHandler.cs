using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Infrastructure.EventHandlers;

public sealed class LowStockDetectedEventHandler(
    IEmailService email,
    ILogger<LowStockDetectedEventHandler> logger)
    : INotificationHandler<LowStockDetectedEvent>
{
    public async Task Handle(LowStockDetectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await email.SendLowStockAlertAsync(
                notification.ProductId,
                notification.CurrentStock,
                notification.Threshold,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send low stock alert for ProductId {ProductId}",
                notification.ProductId);
        }
    }
}
