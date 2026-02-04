using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class DuplicateEmailException(string email)
    : ConflictException($"A user with email '{email}' already exists.") { }
