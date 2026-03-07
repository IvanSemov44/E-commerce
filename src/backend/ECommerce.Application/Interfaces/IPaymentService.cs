using ECommerce.Application.DTOs.Payments;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for handling payment processing (mocked for Stripe/PayPal).
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Process a payment for an order.
    /// </summary>
    Task<Result<PaymentResponseDto>> ProcessPaymentAsync(ProcessPaymentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment status from payment provider.
    /// </summary>
    Task<Result<PaymentDetailsDto>> GetPaymentDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment status from payment provider with ownership enforcement.
    /// </summary>
    Task<Result<PaymentDetailsDto>> GetPaymentDetailsAsync(Guid orderId, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund a payment.
    /// </summary>
    Task<Result<RefundResponseDto>> RefundPaymentAsync(RefundPaymentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a payment intent exists and its status.
    /// </summary>
    Task<Result<PaymentDetailsDto>> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate payment method is supported.
    /// </summary>
    Task<bool> IsPaymentMethodSupportedAsync(string paymentMethod, CancellationToken cancellationToken = default);
}
