using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class InsufficientStockException(string productName, int requestedQuantity, int availableQuantity)
    : BadRequestException($"Insufficient stock for product '{productName}'. Requested: {requestedQuantity}, Available: {availableQuantity}.") { }
