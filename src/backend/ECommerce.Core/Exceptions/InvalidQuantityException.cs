using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public class InvalidQuantityException : BadRequestException
{
    public InvalidQuantityException(string message)
        : base(message) { }
}
