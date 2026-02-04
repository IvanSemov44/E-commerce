using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when price range parameters are invalid (max price &lt; min price).
/// </summary>
public sealed class InvalidPriceRangeBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance when max price is less than min price.
    /// </summary>
    /// <param name="minPrice">The minimum price value.</param>
    /// <param name="maxPrice">The maximum price value.</param>
    public InvalidPriceRangeBadRequestException(decimal minPrice, decimal maxPrice)
        : base($"Invalid price range: Max price ({maxPrice}) must be greater than or equal to min price ({minPrice}).") { }
}
