using System.Threading;
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Order repository interface for specialized order data access operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// Gets an order by its order number asynchronously.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="trackChanges">Whether to track changes for the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The order if found; otherwise null.</returns>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for a user with pagination asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="skip">Number of orders to skip.</param>
    /// <param name="take">Number of orders to take.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>User's orders.</returns>
    Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId, int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of orders for a user asynchronously.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The count of user's orders.</returns>
    Task<int> GetUserOrdersCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order with all its items asynchronously.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The order with items.</returns>
    Task<Order?> GetWithItemsAsync(Guid orderId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders with pagination at the database level.
    /// </summary>
    /// <param name="skip">Number of orders to skip.</param>
    /// <param name="take">Number of orders to take.</param>
    /// <param name="trackChanges">Whether to track changes for the entities.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Paginated orders.</returns>
    Task<IEnumerable<Order>> GetAllOrdersPaginatedAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of all orders asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The total count of orders.</returns>
    Task<int> GetTotalOrdersCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total revenue across all orders asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The total revenue.</returns>
    Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders trend data for the last N days asynchronously.
    /// </summary>
    /// <param name="days">Number of days to look back.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Dictionary of dates and order counts.</returns>
    Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revenue trend data for the last N days asynchronously.
    /// </summary>
    /// <param name="days">Number of days to look back.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Dictionary of dates and revenue amounts.</returns>
    Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days, CancellationToken cancellationToken = default);
}
