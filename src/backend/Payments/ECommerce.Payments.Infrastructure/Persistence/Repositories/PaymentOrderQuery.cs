using ECommerce.Payments.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payments.Infrastructure.Persistence.Repositories;

public sealed class PaymentOrderQuery(PaymentsDbContext dbContext) : IPaymentOrderQuery
{
    public Task<PaymentOrderSnapshot?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => dbContext.PaymentOrders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new PaymentOrderSnapshot(o.OrderId, o.Amount, o.UserId))
            .FirstOrDefaultAsync(ct);
}
