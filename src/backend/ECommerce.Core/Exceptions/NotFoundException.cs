namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Not Found" errors (404).
/// All specific not found exceptions should inherit from this class.
/// </summary>
/// <remarks>
/// Initializes a new instance of the NotFoundException class.
/// </remarks>
/// <param name="message">The exception message.</param>
public abstract class NotFoundException(string message) : Exception(message)
{
}
