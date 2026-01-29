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
    Task<OrderDetailDto> CreateOrderAsync(Guid userId, CreateOrderDto dto);

    /// <summary>
    /// Get order details by ID.
    /// </summary>
    Task<OrderDetailDto?> GetOrderByIdAsync(Guid id);

    /// <summary>
    /// Get order details by order number.
    /// </summary>
    Task<OrderDetailDto?> GetOrderByNumberAsync(string orderNumber);

    /// <summary>
    /// Get user's orders with pagination.
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Update order status.
    /// </summary>
    Task<OrderDetailDto> UpdateOrderStatusAsync(Guid id, string status);

    /// <summary>
    /// Cancel an order (if not already shipped).
    /// </summary>
    Task<bool> CancelOrderAsync(Guid id);

    /// <summary>
    /// Get all orders (admin only).
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20);
}
