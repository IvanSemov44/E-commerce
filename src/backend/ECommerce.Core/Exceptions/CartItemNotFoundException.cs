using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class CartItemNotFoundException(Guid cartItemId)
    : NotFoundException($"Cart item with ID {cartItemId} not found") { }
