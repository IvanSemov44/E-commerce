using ECommerce.API.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Creating order for user {UserId}", userId);

        var order = await _orderService.CreateOrderAsync(userId, dto, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id },
            ApiResponse<OrderDetailDto>.Ok(order, "Order created successfully"));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving order {OrderId}", id);
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken: cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));
    }

    [HttpGet("number/{orderNumber}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving order by number {OrderNumber}", orderNumber);
        var order = await _orderService.GetOrderByNumberAsync(orderNumber, cancellationToken: cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));
    }

    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, page);

        var result = await _orderService.GetUserOrdersAsync(userId, page, pageSize, cancellationToken: cancellationToken);

        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all orders, page {Page}", page);
        var result = await _orderService.GetAllOrdersAsync(page, pageSize, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<OrderDto>>.Ok(result, "Orders retrieved successfully"));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto statusUpdate, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", id, statusUpdate.Status);
        var order = await _orderService.UpdateOrderStatusAsync(id, statusUpdate.Status, cancellationToken: cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order status updated successfully"));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling order {OrderId}", id);
        var result = await _orderService.CancelOrderAsync(id, cancellationToken: cancellationToken);
        return Ok(ApiResponse<object>.Ok(new object(), "Order cancelled successfully"));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim?.Value == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

