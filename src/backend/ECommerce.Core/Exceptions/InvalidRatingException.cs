using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidRatingException()
    : BadRequestException("Rating must be between 1 and 5") { }
