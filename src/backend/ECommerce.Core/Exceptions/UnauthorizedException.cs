namespace ECommerce.Core.Exceptions;

/// <summary>
/// Base exception for all "Unauthorized" errors (401).
/// All specific unauthorized exceptions should inherit from this class.
/// </summary>
public abstract class UnauthorizedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the UnauthorizedException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    protected UnauthorizedException(string message) : base(message)
    {
    }
}

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

/// <summary>
/// Exception thrown when a user is not authenticated for the requested operation.
/// </summary>
public sealed class UserNotAuthenticatedUnauthorizedException : UnauthorizedException
{
    /// <summary>
    /// Initializes a new instance for unauthenticated user.
    /// </summary>
    public UserNotAuthenticatedUnauthorizedException()
        : base("User is not authenticated. Please log in to access this resource.") { }
}
