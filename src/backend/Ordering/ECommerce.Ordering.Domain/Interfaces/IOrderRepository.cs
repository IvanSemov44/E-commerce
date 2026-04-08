using ECommerce.Ordering.Domain.Aggregates.Order;

namespace ECommerce.Ordering.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task<int> GetTotalOrdersCountAsync(CancellationToken ct = default);
    Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default);
    Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days, CancellationToken ct = default);
    Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
