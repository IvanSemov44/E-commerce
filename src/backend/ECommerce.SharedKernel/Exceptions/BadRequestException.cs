namespace ECommerce.SharedKernel.Exceptions;

/// <summary>
/// Base exception for all "Bad Request" errors (400).
/// All specific bad request exceptions should inherit from this class.
/// </summary>
public abstract class BadRequestException : Exception
{
    protected BadRequestException() { }
    protected BadRequestException(string message) : base(message) { }
    protected BadRequestException(string message, Exception inner) : base(message, inner) { }
}
