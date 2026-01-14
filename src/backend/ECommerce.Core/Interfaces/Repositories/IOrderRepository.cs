using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId, int skip, int take);
    Task<int> GetUserOrdersCountAsync(Guid userId);
    Task<Order?> GetWithItemsAsync(Guid orderId);
}
