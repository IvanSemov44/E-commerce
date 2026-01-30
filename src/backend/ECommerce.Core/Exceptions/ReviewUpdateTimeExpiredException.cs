namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to update a review after the allowed time window.
/// </summary>
public sealed class ReviewUpdateTimeExpiredException : BadRequestException
{
    public ReviewUpdateTimeExpiredException()
        : base("Reviews can only be updated within 24 hours of creation") { }

    public ReviewUpdateTimeExpiredException(string message)
        : base(message) { }
}
