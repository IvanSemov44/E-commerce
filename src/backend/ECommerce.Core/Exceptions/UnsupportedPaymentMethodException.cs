namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when an unsupported payment method is provided.
/// </summary>
public sealed class UnsupportedPaymentMethodException : BadRequestException
{
    public UnsupportedPaymentMethodException(string paymentMethod)
        : base($"Payment method '{paymentMethod}' is not supported") { }

    public UnsupportedPaymentMethodException(string message, string paymentMethod)
        : base(message) { }
}
