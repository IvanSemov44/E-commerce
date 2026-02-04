using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

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
