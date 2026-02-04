using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class PaymentAmountMismatchException(decimal expectedAmount, decimal providedAmount)
    : BadRequestException($"Payment amount mismatch. Expected: {expectedAmount:C}, Provided: {providedAmount:C}") { }
