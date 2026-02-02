using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for handling payment processing (mocked for Stripe/PayPal).
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Process a payment for an order.
    /// </summary>
    Task<PaymentResponseDto> ProcessPaymentAsync(ProcessPaymentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment status from payment provider.
    /// </summary>
    Task<PaymentDetailsDto> GetPaymentDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund a payment.
    /// </summary>
    Task<RefundResponseDto> RefundPaymentAsync(RefundPaymentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a payment intent exists and its status.
    /// </summary>
    Task<PaymentDetailsDto?> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate payment method is supported.
    /// </summary>
    Task<bool> IsPaymentMethodSupportedAsync(string paymentMethod, CancellationToken cancellationToken = default);
}
