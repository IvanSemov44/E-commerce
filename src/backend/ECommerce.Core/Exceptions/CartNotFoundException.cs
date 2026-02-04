using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class CartNotFoundException(Guid userId)
    : NotFoundException($"Cart for user with ID '{userId}' was not found.") { }
