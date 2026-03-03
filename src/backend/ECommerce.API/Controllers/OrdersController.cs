using ECommerce.API.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Common;
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
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ICurrentUserService currentUser, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order for the authenticated user from their shopping cart.
    /// </summary>
    /// <param name="dto">The order creation details including shipping and payment information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created order with all details.</returns>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Invalid order data or cart validation failed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Cart or products not found.</response>
    [HttpPost]
    [AllowAnonymous]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrNull;
        _logger.LogInformation("Creating order for user {UserId}. Guest: {IsGuest}", userId, userId == null);

        var order = await _orderService.CreateOrderAsync(userId, dto, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id },
            ApiResponse<OrderDetailDto>.Ok(order, "Order created successfully"));
    }

    /// <summary>
    /// Retrieves an order by its ID.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order details.</returns>
    /// <response code="200">Order retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving order {OrderId}", id);
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken: cancellationToken);
        if (order == null)
        {
            return NotFound(ApiResponse<OrderDetailDto>.Failure(new ErrorResponse
            {
                Message = "Order not found",
                Code = "ORDER_NOT_FOUND"
            }));
        }

        // Ownership check: only order owner or admin can view
        var currentUserId = _currentUser.UserIdOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                     (_currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin);

        if (!isAdmin && order.UserId != currentUserId)
        {
            _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OrderOwnerId}",
                currentUserId, id, order.UserId);
            return Forbid();
        }

        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));
    }

    /// <summary>
    /// Retrieves an order by its order number.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order details.</returns>
    /// <response code="200">Order retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("number/{orderNumber}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving order by number {OrderNumber}", orderNumber);
        var order = await _orderService.GetOrderByNumberAsync(orderNumber, cancellationToken: cancellationToken);
        if (order == null)
        {
            return NotFound(ApiResponse<OrderDetailDto>.Failure(new ErrorResponse
            {
                Message = "Order not found",
                Code = "ORDER_NOT_FOUND"
            }));
        }

        // Ownership check: only order owner or admin can view
        var currentUserId = _currentUser.UserIdOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                     (_currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin);

        if (!isAdmin && order.UserId != currentUserId)
        {
            _logger.LogWarning("User {UserId} attempted to access order {OrderNumber} belonging to {OrderOwnerId}",
                currentUserId, orderNumber, order.UserId);
            return Forbid();
        }

        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a paginated list of orders for the authenticated user.
    /// </summary>
    /// <param name="parameters">Query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of the user's orders.</returns>
    /// <response code="200">Orders retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, parameters.Page);

        var result = await _orderService.GetUserOrdersAsync(userId, parameters, cancellationToken: cancellationToken);

        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a paginated list of all orders in the system (admin only).
    /// </summary>
    /// <param name="parameters">Query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of all orders.</returns>
    /// <response code="200">Orders retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view all orders.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all orders, page {Page}", parameters.Page);
        var result = await _orderService.GetAllOrdersAsync(parameters, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
    }

    /// <summary>
    /// Updates the status of an order (admin only).
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="statusUpdate">The new order status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order.</returns>
    /// <response code="200">Order status updated successfully.</response>
    /// <response code="400">Invalid status or status transition not allowed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update order status.</response>
    /// <response code="404">Order not found.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto statusUpdate, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", id, statusUpdate.Status);
        var order = await _orderService.UpdateOrderStatusAsync(id, statusUpdate.Status, cancellationToken: cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order status updated successfully"));
    }

    /// <summary>
    /// Cancels an order if it hasn't been shipped yet.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cancellation result.</returns>
    /// <response code="200">Order cancelled successfully.</response>
    /// <response code="400">Order cannot be cancelled because it has already been shipped or delivered.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to cancel this order.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling order {OrderId}", id);

        // Ownership check: retrieve order first
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken: cancellationToken);
        if (order == null)
        {
            return NotFound(ApiResponse<object>.Failure("Order not found", "ORDER_NOT_FOUND"));
        }

        // Check if user owns the order or is admin
        var currentUserId = _currentUser.UserIdOrNull;
        var isAdmin = _currentUser.IsAuthenticated &&
                     (_currentUser.Role == Core.Enums.UserRole.Admin || _currentUser.Role == Core.Enums.UserRole.SuperAdmin);

        if (!isAdmin && order.UserId != currentUserId)
        {
            _logger.LogWarning("User {UserId} attempted to cancel order {OrderId} belonging to {OrderOwnerId}",
                currentUserId, id, order.UserId);
            return StatusCode(403, ApiResponse<object>.Failure("You do not have permission to cancel this order", "INSUFFICIENT_PERMISSIONS"));
        }

        var result = await _orderService.CancelOrderAsync(id, cancellationToken: cancellationToken);
        return Ok(ApiResponse<object>.Ok(new object(), "Order cancelled successfully"));
    }
}


