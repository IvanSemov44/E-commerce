using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class OrderNotFoundException(Guid orderId)
    : NotFoundException($"Order with ID '{orderId}' was not found.") { }
