using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Services;
using ECommerce.Application.Interfaces;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for order management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order.
    /// </summary>
    /// <param name="dto">Order creation details.</param>
    /// <returns>Created order details.</returns>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Invalid order creation request.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OrderDetailDto>.Error("Validation failed", errors));
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Creating order for user {UserId}", userId);

            var order = await _orderService.CreateOrderAsync(userId, dto);

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id },
                ApiResponse<OrderDetailDto>.Ok(order, "Order created successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Order creation validation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<OrderDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, ApiResponse<OrderDetailDto>.Error("An error occurred while creating the order"));
        }
    }

    /// <summary>
    /// Get order details by ID.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>Order details.</returns>
    /// <response code="200">Order retrieved successfully.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving order {OrderId}", id);

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(ApiResponse<OrderDetailDto>.Error("Order not found"));
            }

            return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, ApiResponse<OrderDetailDto>.Error("An error occurred while retrieving the order"));
        }
    }

    /// <summary>
    /// Get order by order number.
    /// </summary>
    /// <param name="orderNumber">The order number (e.g., ORD-20250120-ABC123).</param>
    /// <returns>Order details.</returns>
    /// <response code="200">Order retrieved successfully.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("number/{orderNumber}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber)
    {
        try
        {
            _logger.LogInformation("Retrieving order by number {OrderNumber}", orderNumber);

            var order = await _orderService.GetOrderByNumberAsync(orderNumber);
            if (order == null)
            {
                return NotFound(ApiResponse<OrderDetailDto>.Error("Order not found"));
            }

            return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderNumber}", orderNumber);
            return StatusCode(500, ApiResponse<OrderDetailDto>.Error("An error occurred while retrieving the order"));
        }
    }

    /// <summary>
    /// Get user's orders with pagination.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <returns>Paginated list of user's orders.</returns>
    /// <response code="200">Orders retrieved successfully.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, page);

            var result = await _orderService.GetUserOrdersAsync(userId, page, pageSize);

            return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user orders");
            return StatusCode(500, ApiResponse<PaginatedResult<OrderDto>>.Error("An error occurred while retrieving orders"));
        }
    }

    /// <summary>
    /// Get all orders (admin only).
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <returns>Paginated list of all orders.</returns>
    /// <response code="200">Orders retrieved successfully.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="403">Forbidden - requires admin role.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving all orders, page {Page}", page);

            var result = await _orderService.GetAllOrdersAsync(page, pageSize);

            return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, ApiResponse<PaginatedResult<OrderDto>>.Error("An error occurred while retrieving orders"));
        }
    }

    /// <summary>
    /// Update order status (admin only).
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="statusUpdate">The new order status.</param>
    /// <returns>Updated order details.</returns>
    /// <response code="200">Order status updated successfully.</response>
    /// <response code="400">Invalid status or request.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="403">Forbidden - requires admin role.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto statusUpdate)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OrderDetailDto>.Error("Validation failed", errors));
            }

            _logger.LogInformation("Updating order {OrderId} status to {Status}", id, statusUpdate.Status);

            var order = await _orderService.UpdateOrderStatusAsync(id, statusUpdate.Status);

            return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order status updated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Order status update validation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<OrderDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(500, ApiResponse<OrderDetailDto>.Error("An error occurred while updating the order"));
        }
    }

    /// <summary>
    /// Cancel an order (admin only or order owner).
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>Cancellation result.</returns>
    /// <response code="200">Order cancelled successfully.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="400">Order cannot be cancelled.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        try
        {
            _logger.LogInformation("Cancelling order {OrderId}", id);

            var result = await _orderService.CancelOrderAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Error("Order not found"));
            }

            return Ok(ApiResponse<bool>.Ok(true, "Order cancelled successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot cancel order: {Message}", ex.Message);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return StatusCode(500, ApiResponse<bool>.Error("An error occurred while cancelling the order"));
        }
    }

    /// <summary>
    /// Get the current user's ID from JWT claims.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in token");
        }
        return userId;
    }
}

/// <summary>
/// DTO for updating order status.
/// </summary>
public class UpdateOrderStatusDto
{
    public string Status { get; set; } = null!;
}
