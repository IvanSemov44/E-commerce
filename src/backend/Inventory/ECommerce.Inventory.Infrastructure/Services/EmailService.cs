using ECommerce.Inventory.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Inventory.Infrastructure.Services;

public class EmailService(ILogger<EmailService> _logger) : IEmailService
{
    public Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken ct)
    {
        _logger.LogWarning(
            "LOW STOCK ALERT: ProductId={ProductId} has {CurrentStock} units (threshold={Threshold})",
            productId, currentStock, threshold);

        return Task.CompletedTask;
    }
}