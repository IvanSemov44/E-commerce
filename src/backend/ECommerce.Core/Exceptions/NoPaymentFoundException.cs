using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class NoPaymentFoundException(Guid orderId)
    : NotFoundException($"No payment found for order {orderId}") { }
