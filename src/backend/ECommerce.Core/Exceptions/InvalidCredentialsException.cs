using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidCredentialsException()
    : UnauthorizedException("Invalid email or password.") { }
