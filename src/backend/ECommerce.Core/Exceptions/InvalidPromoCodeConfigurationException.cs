using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a promo code configuration is invalid.
/// </summary>
public sealed class InvalidPromoCodeConfigurationException : BadRequestException
{
    public InvalidPromoCodeConfigurationException(string message)
        : base(message) { }
}
