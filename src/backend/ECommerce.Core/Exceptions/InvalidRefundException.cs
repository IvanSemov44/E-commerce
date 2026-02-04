using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a refund request is invalid.
/// </summary>
public sealed class InvalidRefundException : BadRequestException
{
    public InvalidRefundException(string message)
        : base(message) { }
}
