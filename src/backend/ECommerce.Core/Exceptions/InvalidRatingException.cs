namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a review rating is invalid.
/// </summary>
public sealed class InvalidRatingException : BadRequestException
{
    public InvalidRatingException()
        : base("Rating must be between 1 and 5") { }
}
