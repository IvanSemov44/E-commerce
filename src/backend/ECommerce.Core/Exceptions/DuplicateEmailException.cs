namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to register with an email that already exists.
/// </summary>
public sealed class DuplicateEmailException : ConflictException
{
    public DuplicateEmailException(string email)
        : base($"A user with email '{email}' already exists.")
    {
    }
}
