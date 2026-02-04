using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidQuantityException(string message)
    : BadRequestException(message) { }
