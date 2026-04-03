using MediatR;
using Microsoft.Extensions.Logging;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;

namespace ECommerce.Inventory.Application.EventHandlers;

public class SendLowStockAlertOnLowStockDetectedHandler(
    IEmailService _email,
    ILogger<SendLowStockAlertOnLowStockDetectedHandler> _logger
) : INotificationHandler<LowStockDetectedEvent>
{
    public async Task Handle(LowStockDetectedEvent notification, CancellationToken ct)
    {
        try
        {
            await _email.SendLowStockAlertAsync(
                notification.ProductId,
                notification.CurrentStock,
                notification.Threshold,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send low stock alert for ProductId {ProductId}",
                notification.ProductId);
        }
    }
}