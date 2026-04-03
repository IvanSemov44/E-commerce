using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Inventory.Domain.Errors;
using ECommerce.Inventory.Domain.Events;
using ECommerce.Inventory.Domain.ValueObjects;

namespace ECommerce.Inventory.Domain.Aggregates.InventoryItem;

public sealed class InventoryItem : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public StockLevel Stock { get; private set; } = null!;
    public int LowStockThreshold { get; private set; }
    public bool TrackInventory { get; private set; }

    private readonly List<InventoryLog> _logEntries = new();
    public IReadOnlyCollection<InventoryLog> Log => _logEntries.AsReadOnly();

    private InventoryItem() { }

    public static Result<InventoryItem> Create(Guid productId, int initialQuantity, int lowStockThreshold)
    {
        if (lowStockThreshold < 0)
            return Result<InventoryItem>.Fail(InventoryErrors.ThresholdNegative);

        var stockResult = StockLevel.Create(initialQuantity);
        if (!stockResult.IsSuccess)
            return Result<InventoryItem>.Fail(stockResult.GetErrorOrThrow());

        InventoryItem item = new()
        {
            ProductId = productId,
            Stock = stockResult.GetDataOrThrow(),
            LowStockThreshold = lowStockThreshold,
            TrackInventory = true,
        };

        return Result<InventoryItem>.Ok(item);
    }

    public Result Reduce(int amount, string reason)
    {
        var previous = Stock;
        var reduceResult = Stock.Reduce(amount);
        if (!reduceResult.IsSuccess)
            return Result.Fail(reduceResult.GetErrorOrThrow());

        Stock = reduceResult.GetDataOrThrow();
        _logEntries.Add(InventoryLog.Create(Id, -amount, reason, Stock.Quantity));
        AddDomainEvent(new StockReducedEvent(Id, ProductId, amount, Stock.Quantity, reason));

        if (Stock.Quantity <= LowStockThreshold && previous.Quantity > LowStockThreshold)
            AddDomainEvent(new LowStockDetectedEvent(ProductId, Stock.Quantity, LowStockThreshold));

        return Result.Ok();
    }

    public Result Increase(int amount, string reason)
    {
        var increaseResult = Stock.Increase(amount);
        if (!increaseResult.IsSuccess)
            return Result.Fail(increaseResult.GetErrorOrThrow());

        Stock = increaseResult.GetDataOrThrow();
        _logEntries.Add(InventoryLog.Create(Id, amount, reason, Stock.Quantity));
        AddDomainEvent(new StockReplenishedEvent(ProductId, amount, Stock.Quantity));
        return Result.Ok();
    }

    public Result Adjust(int newQuantity, string reason)
    {
        var stockResult = StockLevel.Create(newQuantity);
        if (!stockResult.IsSuccess)
            return Result.Fail(stockResult.GetErrorOrThrow());

        int delta = newQuantity - Stock.Quantity;
        Stock = stockResult.GetDataOrThrow();
        _logEntries.Add(InventoryLog.Create(Id, delta, reason, Stock.Quantity));
        return Result.Ok();
    }
}