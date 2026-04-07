using ECommerce.SharedKernel.Constants;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Payments.Application.Errors;

public static class PaymentsApplicationErrors
{
    public static readonly DomainError InvalidIdempotencyKey = new("INVALID_IDEMPOTENCY_KEY", "Idempotency-Key header is required and must be a valid UUID.");
    public static readonly DomainError IdempotencyInProgress = new("IDEMPOTENCY_IN_PROGRESS", "Request with this idempotency key is already being processed.");
    public static readonly DomainError InternalError = new("INTERNAL_ERROR", "Unknown error occurred.");

    public static readonly DomainError OrderNotFound = new(ErrorCodes.OrderNotFound, "Order not found.");
    public static readonly DomainError UnsupportedPaymentMethod = new(ErrorCodes.UnsupportedPaymentMethod, "Unsupported payment method.");
    public static readonly DomainError PaymentAmountMismatch = new(ErrorCodes.PaymentAmountMismatch, "Payment amount does not match order total.");
    public static readonly DomainError NoPaymentFound = new(ErrorCodes.NoPaymentFound, "No payment found for this order.");
    public static readonly DomainError PaymentIntentNotFound = new(ErrorCodes.PaymentIntentNotFound, "Payment intent not found.");
    public static readonly DomainError InvalidRefund = new(ErrorCodes.InvalidRefund, "Invalid refund request.");
    public static readonly DomainError Forbidden = new(ErrorCodes.Forbidden, "You do not have permission to view payment details for this order.");
    public static readonly DomainError ConcurrencyConflict = new(ErrorCodes.ConcurrencyConflict, "Payment update conflicted with another request. Please retry.");
    public static readonly DomainError PaymentDeclined = new("PAYMENT_DECLINED", "Payment declined. Please check your payment details and try again.");
}
