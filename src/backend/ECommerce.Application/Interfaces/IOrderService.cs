using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing orders.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Create a new order from cart items. Supports both authenticated and guest checkout.
    /// </summary>
    /// <param name="userId">User ID if authenticated, null for guest checkout.</param>
    /// <param name="dto">Order creation DTO. GuestEmail is required if userId is null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<OrderDetailDto>> CreateOrderAsync(Guid? userId, CreateOrderDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order details by ID.
    /// </summary>
    Task<OrderDetailDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order details by order number.
    /// </summary>
    Task<OrderDetailDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's orders with pagination.
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, OrderQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update order status.
    /// </summary>
    Task<Result<OrderDetailDto>> UpdateOrderStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an order (if not already shipped).
    /// </summary>
    Task<Result<Unit>> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all orders (admin only).
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(OrderQueryParameters parameters, CancellationToken cancellationToken = default);
}
