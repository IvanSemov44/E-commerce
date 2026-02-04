using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when trying to register a user that already exists.
/// </summary>
public sealed class UserAlreadyExistsBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance when email already exists.
    /// </summary>
    /// <param name="email">The email that already exists in the system.</param>
    public UserAlreadyExistsBadRequestException(string email)
        : base($"User with email '{email}' already exists.") { }
}
