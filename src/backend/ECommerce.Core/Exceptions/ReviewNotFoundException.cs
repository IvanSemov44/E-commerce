using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a review is not found.
/// </summary>
public sealed class ReviewNotFoundException : NotFoundException
{
    public ReviewNotFoundException(Guid reviewId)
        : base($"Review with ID '{reviewId}' was not found.")
    {
    }
}
