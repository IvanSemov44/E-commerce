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

/// <summary>
/// Exception thrown when price range parameters are invalid (max price &lt; min price).
/// </summary>
public sealed class InvalidPriceRangeBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance when max price is less than min price.
    /// </summary>
    /// <param name="minPrice">The minimum price value.</param>
    /// <param name="maxPrice">The maximum price value.</param>
    public InvalidPriceRangeBadRequestException(decimal minPrice, decimal maxPrice)
        : base($"Invalid price range: Max price ({maxPrice}) must be greater than or equal to min price ({minPrice}).") { }
}

/// <summary>
/// Exception thrown when credentials are invalid during authentication.
/// </summary>
public sealed class InvalidCredentialsBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance for invalid login credentials.
    /// </summary>
    public InvalidCredentialsBadRequestException()
        : base("Email or password is incorrect.") { }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="message">Custom error message for credentials validation.</param>
    public InvalidCredentialsBadRequestException(string message)
        : base(message) { }
}

/// <summary>
/// Exception thrown when a password change request is invalid.
/// </summary>
public sealed class InvalidPasswordChangeBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance when old password is incorrect.
    /// </summary>
    public InvalidPasswordChangeBadRequestException()
        : base("Current password is incorrect.") { }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="message">The error message describing the password change issue.</param>
    public InvalidPasswordChangeBadRequestException(string message)
        : base(message) { }
}

/// <summary>
/// Exception thrown when trying to register a user that already exists.
/// </summary>
public sealed class UserAlreadyExistsBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance when email already exists.
    /// </summary>
    /// <param name="email">The email that already exists in the system.</param>
    public UserAlreadyExistsBadRequestException(string email)
        : base($"User with email '{email}' already exists.") { }
}

/// <summary>
/// Exception thrown when pagination parameters are invalid.
/// </summary>
public sealed class InvalidPaginationBadRequestException : BadRequestException
{
    /// <summary>
    /// Initializes a new instance for invalid page number.
    /// </summary>
    /// <param name="pageNumber">The invalid page number.</param>
    public InvalidPaginationBadRequestException(int pageNumber)
        : base($"Invalid page number '{pageNumber}'. Page number must be greater than 0.") { }
}
