using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class ReviewUpdateTimeExpiredException()
    : BadRequestException("Review can no longer be updated as the time window has expired") { }
