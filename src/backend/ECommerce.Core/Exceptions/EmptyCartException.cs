using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class EmptyCartException()
    : BadRequestException("Cannot proceed to checkout with an empty cart.") { }
