namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a refund request is invalid.
/// </summary>
public sealed class InvalidRefundException : BadRequestException
{
    public InvalidRefundException(string message)
        : base(message) { }

    public InvalidRefundException(string paymentStatus)
        : base($"Cannot refund order with payment status: {paymentStatus}") { }
}
