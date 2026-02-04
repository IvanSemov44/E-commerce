using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public class CartItemNotFoundException : NotFoundException
{
    public CartItemNotFoundException(Guid cartItemId)
        : base($"Cart item with ID {cartItemId} not found") { }
}
