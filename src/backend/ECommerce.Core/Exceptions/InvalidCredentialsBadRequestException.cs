using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

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
}
