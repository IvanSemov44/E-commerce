using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class GuestEmailRequiredException()
    : BadRequestException("Guest checkout requires an email address. Please provide guestEmail in the request.") { }
