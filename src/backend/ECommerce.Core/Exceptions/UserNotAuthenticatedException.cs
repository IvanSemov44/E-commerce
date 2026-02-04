using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class UserNotAuthenticatedException()
    : UnauthorizedException("User is not authenticated. Please log in to access this resource.") { }
