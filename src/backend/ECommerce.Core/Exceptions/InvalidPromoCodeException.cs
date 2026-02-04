using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidPromoCodeException(string message)
    : BadRequestException(message) { }
