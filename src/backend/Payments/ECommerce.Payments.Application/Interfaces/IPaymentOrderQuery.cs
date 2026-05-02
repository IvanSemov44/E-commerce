namespace ECommerce.Payments.Application.Interfaces;

public record PaymentOrderSnapshot(Guid OrderId, decimal Amount, Guid UserId);

public interface IPaymentOrderQuery
{
    Task<PaymentOrderSnapshot?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}
