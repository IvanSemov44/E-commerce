namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Not Found" errors (404).
/// All specific not found exceptions should inherit from this class.
/// </summary>
public abstract class NotFoundException : Exception
{
    protected NotFoundException(string message) : base(message)
    {
    }
}
