using ECommerce.SharedKernel.Results;

namespace ECommerce.Payments.Domain.Errors;

public static class PaymentErrors
{
    public static readonly DomainError InvalidTransition = new("PAYMENT_INVALID_TRANSITION", "Payment cannot transition to the requested state.");
    public static readonly DomainError InvalidRefund = new("PAYMENT_INVALID_REFUND", "Only paid payments can be refunded.");
}
