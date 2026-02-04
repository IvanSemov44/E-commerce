using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public class ProductNotAvailableException : BadRequestException
{
    public ProductNotAvailableException(string productName)
        : base($"Product '{productName}' is no longer available") { }
}
