namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Unauthorized" errors (401).
/// All specific unauthorized exceptions should inherit from this class.
/// </summary>
public abstract class UnauthorizedException : Exception
{
    protected UnauthorizedException(string message) : base(message)
    {
    }
}
