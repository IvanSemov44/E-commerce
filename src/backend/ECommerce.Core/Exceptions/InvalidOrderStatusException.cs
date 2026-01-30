namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when an order status transition is invalid.
/// </summary>
public sealed class InvalidOrderStatusException : BadRequestException
{
    public InvalidOrderStatusException(string currentStatus, string newStatus)
        : base($"Cannot change order status from '{currentStatus}' to '{newStatus}'.")
    {
    }

    public InvalidOrderStatusException(string message)
        : base(message)
    {
    }
}
