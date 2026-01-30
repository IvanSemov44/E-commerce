namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a promo code that already exists.
/// </summary>
public sealed class PromoCodeAlreadyExistsException : ConflictException
{
    public PromoCodeAlreadyExistsException(string code)
        : base($"Promo code '{code}' already exists") { }
}
