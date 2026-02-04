using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidPasswordChangeException()
    : BadRequestException("Current password is incorrect.") { }
