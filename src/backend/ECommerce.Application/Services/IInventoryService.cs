using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Services;

public interface IInventoryService
{
    // Stock Management
    Task<bool> ReduceStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null);
    Task<bool> IncreaseStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null);
    Task<bool> AdjustStockAsync(Guid productId, int newQuantity, string reason, string? notes = null, Guid? userId = null);

    // Stock Validation
    Task<StockCheckResponse> CheckStockAvailabilityAsync(List<StockCheckItem> items);
    Task<bool> IsStockAvailableAsync(Guid productId, int quantity);

    // Inventory Queries
    Task<List<InventoryDto>> GetAllInventoryAsync(int page = 1, int pageSize = 50, string? search = null, bool? lowStockOnly = null);
    Task<List<LowStockAlert>> GetLowStockProductsAsync();
    Task<List<InventoryLogDto>> GetInventoryHistoryAsync(Guid productId, int page = 1, int pageSize = 50);

    // Low Stock Alerts
    Task CheckAndSendLowStockAlertsAsync(Guid productId);
}
