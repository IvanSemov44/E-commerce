namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to update a review past the allowed time window.
/// </summary>
public sealed class ReviewUpdateTimeExpiredException : BadRequestException
{
    public ReviewUpdateTimeExpiredException()
        : base("Review can no longer be updated as the time window has expired") { }
}
