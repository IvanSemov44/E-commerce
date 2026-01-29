namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a promo code is invalid or cannot be applied.
/// </summary>
public sealed class InvalidPromoCodeException : BadRequestException
{
    public InvalidPromoCodeException(string message)
        : base(message)
    {
    }
}
