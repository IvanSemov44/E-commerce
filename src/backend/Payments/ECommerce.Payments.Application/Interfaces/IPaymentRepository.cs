using ECommerce.Payments.Domain.Aggregates.Payment;

namespace ECommerce.Payments.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task AddAsync(Payment payment, CancellationToken ct = default);
}
