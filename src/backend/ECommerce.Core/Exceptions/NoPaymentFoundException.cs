namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when no payment is found for an order.
/// </summary>
public sealed class NoPaymentFoundException : NotFoundException
{
    public NoPaymentFoundException(Guid orderId)
        : base($"No payment found for order {orderId}") { }
}
