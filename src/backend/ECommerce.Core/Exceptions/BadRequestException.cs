namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Bad Request" errors (400).
/// All specific bad request exceptions should inherit from this class.
/// </summary>
public abstract class BadRequestException : Exception
{
    protected BadRequestException(string message) : base(message)
    {
    }
}
