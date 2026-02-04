using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class PromoCodeAlreadyExistsException(string code)
    : ConflictException($"Promo code '{code}' already exists") { }
