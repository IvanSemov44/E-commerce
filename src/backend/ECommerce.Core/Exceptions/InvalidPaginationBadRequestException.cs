using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when pagination parameters are invalid.
/// </summary>
public sealed class InvalidPaginationBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance for invalid page number.
    /// </summary>
    /// <param name="pageNumber">The invalid page number.</param>
    public InvalidPaginationBadRequestException(int pageNumber)
        : base($"Invalid page number '{pageNumber}'. Page number must be greater than 0.") { }
}
