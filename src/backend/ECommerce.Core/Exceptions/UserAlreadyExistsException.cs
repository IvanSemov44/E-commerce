using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class UserAlreadyExistsException(string email)
    : BadRequestException($"User with email '{email}' already exists.") { }
