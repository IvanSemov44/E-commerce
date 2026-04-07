using ECommerce.Payments.Application.DTOs;

namespace ECommerce.Payments.Application.Interfaces;

public interface IPaymentStore
{
    Task StorePaymentAsync(string paymentId, PaymentDetailsDto details, CancellationToken cancellationToken = default);
    Task<PaymentDetailsDto?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
    Task RemovePaymentAsync(string paymentId, CancellationToken cancellationToken = default);
}
