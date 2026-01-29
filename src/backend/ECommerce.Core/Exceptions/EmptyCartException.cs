namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to checkout with an empty cart.
/// </summary>
public sealed class EmptyCartException : BadRequestException
{
    public EmptyCartException()
        : base("Cannot proceed to checkout with an empty cart.")
    {
    }
}
