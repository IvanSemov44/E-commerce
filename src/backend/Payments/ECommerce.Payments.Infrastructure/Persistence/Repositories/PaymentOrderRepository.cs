using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Payments.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payments.Infrastructure.Persistence.Repositories;

public sealed class PaymentOrderRepository(AppDbContext dbContext) : IPaymentOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid orderId, bool trackChanges, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders.AsQueryable();
        if (!trackChanges)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Orders.Update(order);
        return Task.CompletedTask;
    }
}
