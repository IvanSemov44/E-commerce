namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when an order is not found.
/// </summary>
public sealed class OrderNotFoundException : NotFoundException
{
    public OrderNotFoundException(Guid orderId)
        : base($"Order with ID '{orderId}' was not found.")
    {
    }
}
