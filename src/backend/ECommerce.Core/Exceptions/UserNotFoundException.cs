namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a user is not found.
/// </summary>
public sealed class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(Guid userId)
        : base($"User with ID '{userId}' was not found.")
    {
    }

    public UserNotFoundException(string email)
        : base($"User with email '{email}' was not found.")
    {
    }
}
