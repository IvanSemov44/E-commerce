using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing orders.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Create a new order from cart items.
    /// </summary>
    Task<OrderDetailDto> CreateOrderAsync(Guid userId, CreateOrderDto dto, CancellationToken cancellationToken = default);

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
    Task<OrderDetailDto> UpdateOrderStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an order (if not already shipped).
    /// </summary>
    Task<bool> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all orders (admin only).
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(OrderQueryParameters parameters, CancellationToken cancellationToken = default);
}
