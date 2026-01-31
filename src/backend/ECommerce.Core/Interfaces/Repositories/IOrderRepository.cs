using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, bool trackChanges = false);
    Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId, int skip, int take, bool trackChanges = false);
    Task<int> GetUserOrdersCountAsync(Guid userId);
    Task<Order?> GetWithItemsAsync(Guid orderId, bool trackChanges = false);
    Task<int> GetTotalOrdersCountAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days);
    Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days);
}
