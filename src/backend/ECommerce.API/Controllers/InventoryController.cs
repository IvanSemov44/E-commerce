using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<ActionResult<object>> GetAllInventory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] bool? lowStockOnly = null)
    {
        try
        {
            var inventory = await _inventoryService.GetAllInventoryAsync(page, pageSize, search, lowStockOnly);
            return Ok(new
            {
                success = true,
                data = inventory,
                page,
                pageSize,
                message = "Inventory retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to retrieve inventory",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get products with low stock.
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<object>> GetLowStockProducts()
    {
        try
        {
            var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();
            return Ok(new
            {
                success = true,
                data = lowStockProducts,
                count = lowStockProducts.Count,
                message = "Low stock products retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to retrieve low stock products",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get inventory history for a specific product.
    /// </summary>
    [HttpGet("{productId}/history")]
    public async Task<ActionResult<object>> GetInventoryHistory(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var history = await _inventoryService.GetInventoryHistoryAsync(productId, page, pageSize);
            return Ok(new
            {
                success = true,
                data = history,
                page,
                pageSize,
                message = "Inventory history retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory history for product {ProductId}", productId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to retrieve inventory history",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Adjust stock quantity for a product.
    /// </summary>
    [HttpPost("{productId}/adjust")]
    public async Task<ActionResult<object>> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request)
    {
        try
        {
            if (request.Quantity < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Quantity cannot be negative",
                    errors = new[] { "Invalid quantity value" }
                });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userGuid = userId != null ? Guid.Parse(userId) : (Guid?)null;

            var result = await _inventoryService.AdjustStockAsync(
                productId,
                request.Quantity,
                request.Reason,
                request.Notes,
                userGuid
            );

            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Product not found",
                    errors = new[] { $"Product with ID {productId} not found" }
                });
            }

            return Ok(new
            {
                success = true,
                message = "Stock adjusted successfully",
                data = new { productId, newQuantity = request.Quantity }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock for product {ProductId}", productId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to adjust stock",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Increase stock (restock) for a product.
    /// </summary>
    [HttpPost("{productId}/restock")]
    public async Task<ActionResult<object>> RestockProduct(Guid productId, [FromBody] AdjustStockRequest request)
    {
        try
        {
            if (request.Quantity <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Quantity must be positive",
                    errors = new[] { "Invalid quantity value" }
                });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userGuid = userId != null ? Guid.Parse(userId) : (Guid?)null;

            var result = await _inventoryService.IncreaseStockAsync(
                productId,
                request.Quantity,
                request.Reason ?? "restock",
                null,
                userGuid
            );

            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Product not found",
                    errors = new[] { $"Product with ID {productId} not found" }
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Stock increased by {request.Quantity} units",
                data = new { productId, quantityAdded = request.Quantity }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restocking product {ProductId}", productId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to restock product",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Check stock availability for items (used by storefront before checkout).
    /// </summary>
    [HttpPost("check-availability")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CheckStockAvailability([FromBody] StockCheckRequest request)
    {
        try
        {
            var result = await _inventoryService.CheckStockAvailabilityAsync(request.Items);
            return Ok(new
            {
                success = true,
                data = result,
                message = result.IsAvailable ? "All items are available" : "Some items have stock issues"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock availability");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to check stock availability",
                errors = new[] { ex.Message }
            });
        }
    }
}
