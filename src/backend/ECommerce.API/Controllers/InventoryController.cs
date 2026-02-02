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
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory with pagination and filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllInventory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] bool? lowStockOnly = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving inventory (page: {Page}, pageSize: {PageSize}, search: {Search}, lowStockOnly: {LowStockOnly})",
            page, pageSize, search, lowStockOnly);

        var inventory = await _inventoryService.GetAllInventoryAsync(page, pageSize, search, lowStockOnly, cancellationToken: cancellationToken);
        return Ok(ApiResponse<List<InventoryDto>>.Ok(inventory, "Inventory retrieved successfully"));
    }

    /// <summary>
    /// Get products with low stock.
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<List<LowStockAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLowStockProducts(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving low stock products");

        var lowStockProducts = await _inventoryService.GetLowStockProductsAsync(cancellationToken: cancellationToken);
        return Ok(ApiResponse<List<LowStockAlertDto>>.Ok(lowStockProducts, "Low stock products retrieved successfully"));
    }

    /// <summary>
    /// Get inventory history for a specific product.
    /// </summary>
    [HttpGet("{productId}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
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
    [HttpPost("{productId}/adjust")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

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

        var response = new StockAdjustmentResponseDto
        {
            ProductId = productId,
            NewQuantity = request.Quantity,
            QuantityChanged = request.Quantity, // Note: This is the target quantity, not the delta
            AdjustedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<StockAdjustmentResponseDto>.Ok(response, "Stock adjusted successfully"));
    }

    /// <summary>
    /// Increase stock (restock) for a product.
    /// </summary>
    [HttpPost("{productId}/restock")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestockProduct(Guid productId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

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
    [HttpPost("check-availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<StockCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckStockAvailability([FromBody] StockCheckRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking stock availability for {ItemCount} items", request.Items.Count);

        var result = await _inventoryService.CheckStockAvailabilityAsync(request.Items, cancellationToken: cancellationToken);
        var message = result.IsAvailable ? "All items are available" : "Some items have stock issues";

        return Ok(ApiResponse<StockCheckResponse>.Ok(result, message));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}
