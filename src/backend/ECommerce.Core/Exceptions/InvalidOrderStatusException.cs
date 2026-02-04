using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InvalidOrderStatusException(string currentStatus, string newStatus)
    : BadRequestException($"Cannot change order status from '{currentStatus}' to '{newStatus}'.") { }
