namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when login credentials are invalid.
/// </summary>
public sealed class InvalidCredentialsException : UnauthorizedException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
