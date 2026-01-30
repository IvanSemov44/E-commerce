namespace ECommerce.Core.Exceptions;

public class InvalidQuantityException : BadRequestException
{
    public InvalidQuantityException(string message)
        : base(message) { }
}
