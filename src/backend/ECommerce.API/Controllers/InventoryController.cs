using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for inventory and stock management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        ICurrentUserService currentUser,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory with pagination and filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<InventoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllInventory(
        [FromQuery] InventoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving inventory (page: {Page}, pageSize: {PageSize}, search: {Search}, lowStockOnly: {LowStockOnly})",
            parameters.Page, parameters.PageSize, parameters.Search, parameters.LowStockOnly);

        var inventory = await _inventoryService.GetAllInventoryAsync(parameters, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<InventoryDto>>.Ok(inventory, "Inventory retrieved successfully"));
    }

    /// <summary>
    /// Get products with low stock.
    /// </summary>
    /// <param name="threshold">Optional threshold value (default from config).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<List<LowStockAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int? threshold = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving low stock products with threshold: {Threshold}", threshold);

        var lowStockProducts = await _inventoryService.GetLowStockProductsAsync(cancellationToken: cancellationToken);
        return Ok(ApiResponse<List<LowStockAlertDto>>.Ok(lowStockProducts, "Low stock products retrieved successfully"));
    }

    /// <summary>
    /// Get inventory for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<InventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductStock(Guid productId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving stock for product {ProductId}", productId);

        var stockCheck = await _inventoryService.CheckStockAvailabilityAsync(
            new List<StockCheckItemDto> { new() { ProductId = productId, Quantity = 0 } },
            cancellationToken);

        if (stockCheck.Issues.Any(i => i.Message == "Product not found"))
            return NotFound(ApiResponse<object>.Failure("Product not found", "PRODUCT_NOT_FOUND"));

        var isAvailable = await _inventoryService.IsStockAvailableAsync(productId, 1, cancellationToken);
        var result = new { ProductId = productId, IsAvailable = isAvailable };
        return Ok(ApiResponse<object>.Ok(result, "Product stock retrieved successfully"));
    }

    /// <summary>
    /// Check if a specific quantity of a product is available.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{productId:guid}/available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckAvailableQuantity(Guid productId, [FromQuery] int quantity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking availability for product {ProductId} with quantity {Quantity}", productId, quantity);

        var isAvailable = await _inventoryService.IsStockAvailableAsync(productId, quantity, cancellationToken);
        var result = new { ProductId = productId, RequestedQuantity = quantity, IsAvailable = isAvailable };
        return Ok(ApiResponse<object>.Ok(result, "Availability check completed"));
    }

    /// <summary>
    /// Get inventory history for a specific product.
    /// </summary>
    [HttpGet("{productId}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInventoryHistory(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving inventory history for product {ProductId} (page: {Page}, pageSize: {PageSize})",
            productId, page, pageSize);

        var history = await _inventoryService.GetInventoryHistoryAsync(productId, page, pageSize, cancellationToken: cancellationToken);
        return Ok(ApiResponse<List<InventoryLogDto>>.Ok(history, "Inventory history retrieved successfully"));
    }

    /// <summary>
    /// Adjust stock quantity for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="request">Stock adjustment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{productId}/adjust")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;

        _logger.LogInformation("Adjusting stock for product {ProductId} to {Quantity} (User: {UserId})",
            productId, request.Quantity, userId);

        await _inventoryService.AdjustStockAsync(
            productId,
            request.Quantity,
            request.Reason,
            request.Notes,
            userId,
            cancellationToken: cancellationToken
        );

        // Get the product to calculate actual quantity change
        var product = await _inventoryService.GetProductByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResponse<object>.Failure($"Product with ID {productId} not found", "PRODUCT_NOT_FOUND"));
        }

        var quantityChanged = request.Quantity - product.StockQuantity;

        var response = new StockAdjustmentResponseDto
        {
            ProductId = productId,
            NewQuantity = request.Quantity,
            QuantityChanged = quantityChanged, // Actual delta change
            AdjustedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<StockAdjustmentResponseDto>.Ok(response, "Stock adjusted successfully"));
    }

    /// <summary>
    /// Increase stock (restock) for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="request">Restock request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{productId}/restock")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestockProduct(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;

        _logger.LogInformation("Restocking product {ProductId} with {Quantity} units (User: {UserId})",
            productId, request.Quantity, userId);

        await _inventoryService.IncreaseStockAsync(
            productId,
            request.Quantity,
            request.Reason ?? "restock",
            null,
            userId,
            cancellationToken: cancellationToken
        );

        var response = new StockAdjustmentResponseDto
        {
            ProductId = productId,
            NewQuantity = 0, // Note: Actual new quantity not available without fetching product
            QuantityChanged = request.Quantity,
            AdjustedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<StockAdjustmentResponseDto>.Ok(response, $"Stock increased by {request.Quantity} units"));
    }

    /// <summary>
    /// Check stock availability for items (used by storefront before checkout).
    /// </summary>
    /// <param name="request">Stock check request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("check-availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<StockCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckStockAvailability([FromBody] StockCheckRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking stock availability for {ItemCount} items", request.Items.Count);

        var result = await _inventoryService.CheckStockAvailabilityAsync(request.Items, cancellationToken: cancellationToken);
        var message = result.IsAvailable ? "All items are available" : "Some items have stock issues";

        return Ok(ApiResponse<StockCheckResponse>.Ok(result, message));
    }

    /// <summary>
    /// Updates stock for a specific product (admin only).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="request">The stock update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProductStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        _logger.LogInformation("Updating stock for product {ProductId} to {Quantity} (User: {UserId})", productId, request.Quantity, userId);

        await _inventoryService.AdjustStockAsync(
            productId,
            request.Quantity,
            request.Reason ?? "stock_update",
            request.Notes,
            userId,
            cancellationToken: cancellationToken
        );

        var response = new StockAdjustmentResponseDto
        {
            ProductId = productId,
            NewQuantity = request.Quantity,
            QuantityChanged = request.Quantity,
            AdjustedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<StockAdjustmentResponseDto>.Ok(response, "Product stock updated successfully"));
    }

    /// <summary>
    /// Bulk updates stock for multiple products (admin only).
    /// </summary>
    /// <param name="request">Bulk update request containing list of updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("bulk-update")]
    [ProducesResponseType(typeof(ApiResponse<List<StockAdjustmentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BulkUpdateStock([FromBody] BulkStockUpdateRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        _logger.LogInformation("Bulk updating stock for {ProductCount} products (User: {UserId})", request.Updates.Count, userId);

        var responses = new List<StockAdjustmentResponseDto>();
        foreach (var update in request.Updates)
        {
            await _inventoryService.AdjustStockAsync(
                update.ProductId,
                update.Quantity,
                "bulk_update",
                null,
                userId,
                cancellationToken: cancellationToken
            );

            responses.Add(new StockAdjustmentResponseDto
            {
                ProductId = update.ProductId,
                NewQuantity = update.Quantity,
                QuantityChanged = update.Quantity,
                AdjustedAt = DateTime.UtcNow
            });
        }

        return Ok(ApiResponse<List<StockAdjustmentResponseDto>>.Ok(responses, "Stock updated successfully"));
    }
}

