namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Not Found" errors (404).
/// All specific not found exceptions should inherit from this class.
/// </summary>
public abstract class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the NotFoundException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    protected NotFoundException(string message) : base(message)
    {
    }
}
