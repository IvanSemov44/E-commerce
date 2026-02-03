namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when payment amount doesn't match order amount.
/// </summary>
public sealed class PaymentAmountMismatchException : BadRequestException
{
    public PaymentAmountMismatchException(decimal expectedAmount, decimal providedAmount)
        : base($"Payment amount mismatch. Expected: {expectedAmount:C}, Provided: {providedAmount:C}") { }
}
