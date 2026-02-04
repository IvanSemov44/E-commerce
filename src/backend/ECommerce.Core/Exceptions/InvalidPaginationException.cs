using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidPaginationException(int pageNumber)
    : BadRequestException($"Invalid page number '{pageNumber}'. Page number must be greater than 0.") { }
