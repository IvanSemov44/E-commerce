namespace ECommerce.Core.Exceptions;

public class InvalidTokenException : UnauthorizedException
{
    public InvalidTokenException()
        : base("Invalid or expired token") { }
}
