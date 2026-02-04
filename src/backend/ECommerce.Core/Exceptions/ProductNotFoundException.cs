using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a product is not found.
/// </summary>
public sealed class ProductNotFoundException : NotFoundException
{
    public ProductNotFoundException(Guid productId)
        : base($"Product with ID '{productId}' was not found.")
    {
    }

    public ProductNotFoundException(string slug)
        : base($"Product with slug '{slug}' was not found.")
    {
    }
}
