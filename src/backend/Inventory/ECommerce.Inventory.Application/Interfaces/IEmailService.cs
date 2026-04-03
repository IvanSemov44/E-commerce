namespace ECommerce.Inventory.Application.Interfaces;

public interface IEmailService
{
    Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken ct);
}