using ECommerce.Core.Entities;

namespace ECommerce.Payments.Application.Interfaces;

public interface IPaymentOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, bool trackChanges, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
