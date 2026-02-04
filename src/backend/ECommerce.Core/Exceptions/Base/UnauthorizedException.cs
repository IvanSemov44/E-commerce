namespace ECommerce.Core.Exceptions.Base;

/// <summary>
/// Base exception for all "Unauthorized" errors (401).
/// All specific unauthorized exceptions should inherit from this class.
/// </summary>
/// <remarks>
/// Initializes a new instance of the UnauthorizedException class.
/// </remarks>
/// <param name="message">The exception message.</param>
public abstract class UnauthorizedException(string message) : Exception(message)
{
}
