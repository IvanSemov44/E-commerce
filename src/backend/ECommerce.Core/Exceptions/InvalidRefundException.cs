using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidRefundException(string message)
    : BadRequestException(message) { }
