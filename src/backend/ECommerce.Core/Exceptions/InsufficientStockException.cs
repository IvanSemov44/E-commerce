using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when there is insufficient stock for a product.
/// </summary>
public sealed class InsufficientStockException : BadRequestException
{
    public InsufficientStockException(string productName, int requestedQuantity, int availableQuantity)
        : base($"Insufficient stock for product '{productName}'. Requested: {requestedQuantity}, Available: {availableQuantity}.")
    {
    }
}
