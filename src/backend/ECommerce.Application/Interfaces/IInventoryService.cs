using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for inventory and stock management.
/// </summary>
public interface IInventoryService
{
    // Stock Management
    Task ReduceStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task IncreaseStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task AdjustStockAsync(Guid productId, int newQuantity, string reason, string? notes = null, Guid? userId = null, CancellationToken cancellationToken = default);

    // Stock Validation
    Task<StockCheckResponse> CheckStockAvailabilityAsync(List<StockCheckItemDto> items, CancellationToken cancellationToken = default);
    Task<bool> IsStockAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    // Inventory Queries
    Task<List<InventoryDto>> GetAllInventoryAsync(int page = 1, int pageSize = 50, string? search = null, bool? lowStockOnly = null, CancellationToken cancellationToken = default);
    Task<List<LowStockAlertDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
    Task<List<InventoryLogDto>> GetInventoryHistoryAsync(Guid productId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    // Low Stock Alerts
    Task CheckAndSendLowStockAlertsAsync(Guid productId, CancellationToken cancellationToken = default);
}
