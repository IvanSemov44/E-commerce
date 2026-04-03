using ECommerce.SharedKernel.Results;

namespace ECommerce.Inventory.Domain.Errors;

public static class InventoryErrors
{
    public static readonly DomainError StockNegative = new("STOCK_NEGATIVE", "Stock quantity cannot be negative.");
    public static readonly DomainError ReduceAmountInvalid = new("REDUCE_AMOUNT_INVALID", "Reduction amount must be greater than zero.");
    public static readonly DomainError IncreaseAmountInvalid = new("INCREASE_AMOUNT_INVALID", "Increase amount must be greater than zero.");
    public static readonly DomainError InsufficientStock = new("INSUFFICIENT_STOCK", "Insufficient stock to complete this operation.");
    public static readonly DomainError ThresholdNegative = new("THRESHOLD_NEGATIVE", "Low stock threshold cannot be negative.");
}