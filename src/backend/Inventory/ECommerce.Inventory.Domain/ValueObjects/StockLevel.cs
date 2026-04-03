using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Domain.Errors;

namespace ECommerce.Inventory.Domain.ValueObjects;

public sealed record StockLevel
{
    public int Quantity { get; }

    private StockLevel() { } // EF Core

    private StockLevel(int quantity) => Quantity = quantity;

    public static Result<StockLevel> Create(int quantity)
    {
        if (quantity < 0)
            return Result<StockLevel>.Fail(InventoryErrors.StockNegative);
        return Result<StockLevel>.Ok(new StockLevel(quantity));
    }

    public static StockLevel Zero => new(0);

    public Result<StockLevel> Reduce(int amount)
    {
        if (amount <= 0)
            return Result<StockLevel>.Fail(InventoryErrors.ReduceAmountInvalid);
        if (Quantity - amount < 0)
            return Result<StockLevel>.Fail(InventoryErrors.InsufficientStock);
        return Result<StockLevel>.Ok(new StockLevel(Quantity - amount));
    }

    public Result<StockLevel> Increase(int amount)
    {
        if (amount <= 0)
            return Result<StockLevel>.Fail(InventoryErrors.IncreaseAmountInvalid);
        return Result<StockLevel>.Ok(new StockLevel(Quantity + amount));
    }
}