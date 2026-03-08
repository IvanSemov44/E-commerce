namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Not Found" errors (404).
/// All specific not found exceptions should inherit from this class.
/// </summary>
/// <remarks>
/// Initializes a new instance of the NotFoundException class.
/// </remarks>
public abstract class NotFoundException : Exception
{
    protected NotFoundException() { }
    protected NotFoundException(string message) : base(message) { }
    protected NotFoundException(string message, Exception inner) : base(message, inner) { }
}
