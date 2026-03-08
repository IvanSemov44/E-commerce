namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Unauthorized" errors (401).
/// All specific unauthorized exceptions should inherit from this class.
/// </summary>
/// <remarks>
/// Initializes a new instance of the UnauthorizedException class.
/// </remarks>
/// <param name="message">The exception message.</param>
public abstract class UnauthorizedException : Exception
{
    protected UnauthorizedException() { }
    protected UnauthorizedException(string message) : base(message) { }
    protected UnauthorizedException(string message, Exception inner) : base(message, inner) { }
}
