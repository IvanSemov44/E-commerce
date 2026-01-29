namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a cart is not found.
/// </summary>
public sealed class CartNotFoundException : NotFoundException
{
    public CartNotFoundException(Guid userId)
        : base($"Cart for user with ID '{userId}' was not found.")
    {
    }
}
