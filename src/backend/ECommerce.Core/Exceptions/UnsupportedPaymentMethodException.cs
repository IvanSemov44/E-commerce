namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when an unsupported payment method is used.
/// </summary>
public sealed class UnsupportedPaymentMethodException : BadRequestException
{
    public UnsupportedPaymentMethodException(string paymentMethod)
        : base($"Payment method '{paymentMethod}' is not supported") { }
}
