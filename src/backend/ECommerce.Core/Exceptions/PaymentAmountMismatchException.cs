namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when payment amount does not match the order total.
/// </summary>
public sealed class PaymentAmountMismatchException : BadRequestException
{
    public PaymentAmountMismatchException(decimal expectedAmount, decimal providedAmount)
        : base($"Payment amount does not match order total. Expected: {expectedAmount:C}, Got: {providedAmount:C}") { }

    public PaymentAmountMismatchException(string message)
        : base(message) { }
}
