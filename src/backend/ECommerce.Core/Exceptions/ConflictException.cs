namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Conflict" errors (409).
/// Used when a request conflicts with the current state (e.g., duplicate resources).
/// All specific conflict exceptions should inherit from this class.
/// </summary>
public abstract class ConflictException : Exception
{
    protected ConflictException(string message) : base(message)
    {
    }
}
