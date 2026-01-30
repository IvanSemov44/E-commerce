namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to create a duplicate review for a product.
/// </summary>
public sealed class DuplicateReviewException : ConflictException
{
    public DuplicateReviewException()
        : base("You have already reviewed this product") { }

    public DuplicateReviewException(Guid userId, Guid productId)
        : base($"User {userId} has already reviewed product {productId}") { }
}
