using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class ProductNotAvailableException(string productName)
    : BadRequestException($"Product '{productName}' is no longer available") { }
