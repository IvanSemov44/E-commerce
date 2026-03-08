using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for inventory and stock management.
/// </summary>
public interface IInventoryService
{
    // Stock Management
    Task<Result<Unit>> ReduceStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<Result<Unit>> ReduceStockBatchAsync(List<(Guid ProductId, int Quantity, string Reason, Guid? ReferenceId, Guid? UserId)> items, CancellationToken cancellationToken = default);
    Task<Result<Unit>> IncreaseStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<Result<Unit>> AdjustStockAsync(Guid productId, int newQuantity, string reason, string? notes = null, Guid? userId = null, CancellationToken cancellationToken = default);

    // Stock Validation
    Task<StockCheckResponse> CheckStockAvailabilityAsync(List<StockCheckItemDto> items, CancellationToken cancellationToken = default);
    Task<bool> IsStockAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    // Low Stock Alerts
    Task CheckAndSendLowStockAlertsAsync(Guid productId, CancellationToken cancellationToken = default);

    // Inventory Queries
    Task<PaginatedResult<InventoryDto>> GetAllInventoryAsync(InventoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<PaginatedResult<LowStockAlertDto>> GetLowStockProductsAsync(int page, int pageSize, int? threshold = null, CancellationToken cancellationToken = default);
    Task<PaginatedResult<InventoryLogDto>> GetInventoryHistoryAsync(Guid productId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<Result<InventoryDto>> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
