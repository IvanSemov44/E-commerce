using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidTokenException()
    : UnauthorizedException("Invalid or expired token") { }
