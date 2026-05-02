using ECommerce.Payments.Application.Interfaces;
using ECommerce.Payments.Domain.Aggregates.Payment;
using ECommerce.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payments.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(PaymentsDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        dbContext.Payments.Add(payment);
        return Task.CompletedTask;
    }
}
