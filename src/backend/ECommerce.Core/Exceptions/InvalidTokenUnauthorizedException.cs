using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when the JWT token is invalid or expired.
/// </summary>
public sealed class InvalidTokenUnauthorizedException : UnauthorizedException
{
    /// <summary>
    /// Initializes a new instance for invalid or expired token.
    /// </summary>
    public InvalidTokenUnauthorizedException()
        : base("The token is invalid or has expired.") { }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="message">The error message describing the token issue.</param>
    public InvalidTokenUnauthorizedException(string message)
        : base(message) { }
}
