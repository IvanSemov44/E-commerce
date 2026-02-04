using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

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
}
