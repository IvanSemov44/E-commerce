using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidPriceRangeException(decimal minPrice, decimal maxPrice)
    : BadRequestException($"Invalid price range: Max price ({maxPrice}) must be greater than or equal to min price ({minPrice}).") { }
