using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Application.Queries.GetInventory;
using ECommerce.Inventory.Application.Queries.GetInventoryByProductId;
using ECommerce.Inventory.Application.Queries.GetInventoryHistory;
using ECommerce.Inventory.Application.Queries.GetLowStockItems;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;

namespace ECommerce.Inventory.Tests.Application;

[TestClass]
public class QueryHandlerTests
{
    private static InventoryItem MakeItem(Guid productId, int quantity = 50, int threshold = 10)
        => InventoryItem.Create(productId, quantity, threshold).GetDataOrThrow();

    [TestMethod]
    public async Task GetInventory_EmptyStore_ReturnsEmptyList()
    {
        var repo = new FakeInventoryItemRepository();
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.GetDataOrThrow());
    }

    [TestMethod]
    public async Task GetInventory_MultipleItems_ReturnsAll()
    {
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(Guid.NewGuid(), 100, 10));
        repo.Store.Add(MakeItem(Guid.NewGuid(), 5, 10));
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.GetDataOrThrow());
    }

    [TestMethod]
    public async Task GetInventory_LowStockOnly_ReturnsOnlyLowStockItems()
    {
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(Guid.NewGuid(), 100, 10));
        repo.Store.Add(MakeItem(Guid.NewGuid(), 5, 10));
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(LowStockOnly: true), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.GetDataOrThrow());
        Assert.IsTrue(result.GetDataOrThrow()[0].IsLowStock);
    }

    [TestMethod]
    public async Task GetInventory_DtoFields_AreCorrectlyMapped()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(productId, quantity: 8, threshold: 10));
        var handler = new GetInventoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryQuery(), default);
        var dto = result.GetDataOrThrow()[0];

        Assert.AreEqual(productId, dto.ProductId);
        Assert.AreEqual(8, dto.Quantity);
        Assert.AreEqual(10, dto.LowStockThreshold);
        Assert.IsTrue(dto.IsLowStock);
        Assert.IsFalse(dto.IsOutOfStock);
    }

    [TestMethod]
    public async Task GetInventoryByProductId_ExistingProduct_ReturnsDto()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(productId, quantity: 50));
        var handler = new GetInventoryByProductIdQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryByProductIdQuery(productId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(productId, result.GetDataOrThrow().ProductId);
        Assert.AreEqual(50, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public async Task GetInventoryByProductId_UnknownProduct_ReturnsNotFound()
    {
        var repo = new FakeInventoryItemRepository();
        var handler = new GetInventoryByProductIdQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryByProductIdQuery(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetInventoryHistory_ExistingProduct_ReturnsLogEntries()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var item = MakeItem(productId, quantity: 50);
        item.Reduce(10, "sale");
        item.Increase(5, "restock");
        repo.Store.Add(item);
        var handler = new GetInventoryHistoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryHistoryQuery(productId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.GetDataOrThrow());
    }

    [TestMethod]
    public async Task GetInventoryHistory_UnknownProduct_ReturnsNotFound()
    {
        var repo = new FakeInventoryItemRepository();
        var handler = new GetInventoryHistoryQueryHandler(repo);

        var result = await handler.Handle(new GetInventoryHistoryQuery(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetLowStockItems_WithThresholdOverride_ReturnsItemsBelowOverride()
    {
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(Guid.NewGuid(), quantity: 15, threshold: 10));
        repo.Store.Add(MakeItem(Guid.NewGuid(), quantity: 5, threshold: 10));
        var handler = new GetLowStockItemsQueryHandler(repo);

        var result = await handler.Handle(new GetLowStockItemsQuery(ThresholdOverride: 20), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.GetDataOrThrow());
    }
}
