using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidPromoCodeConfigurationException(string message)
    : BadRequestException(message) { }
