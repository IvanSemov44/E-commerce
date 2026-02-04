using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class UnsupportedPaymentMethodException(string paymentMethod)
    : BadRequestException($"Payment method '{paymentMethod}' is not supported") { }
