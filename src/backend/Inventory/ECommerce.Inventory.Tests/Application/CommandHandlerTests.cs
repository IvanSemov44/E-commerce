using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Inventory.Application.Commands.IncreaseStock;
using ECommerce.Inventory.Application.Commands.ReduceStock;
using ECommerce.Inventory.Application.Commands.AdjustStock;
using ECommerce.Inventory.Application.Errors;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Errors;

namespace ECommerce.Inventory.Tests.Application;

[TestClass]
public class CommandHandlerTests
{
    private static InventoryItem MakeItem(Guid productId, int quantity = 50, int threshold = 10)
        => InventoryItem.Create(productId, quantity, threshold).GetDataOrThrow();

    [TestMethod]
    public async Task IncreaseStock_ExistingItem_IncreasesQuantityAndSaves()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new IncreaseStockCommandHandler(repo, uow, NullLogger<IncreaseStockCommandHandler>.Instance);
        var result = await handler.Handle(new IncreaseStockCommand(productId, 20, "restock"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, result.GetDataOrThrow().NewQuantity);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task IncreaseStock_UnknownProduct_ReturnsInventoryItemNotFound()
    {
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();

        var handler = new IncreaseStockCommandHandler(repo, uow, NullLogger<IncreaseStockCommandHandler>.Instance);
        var result = await handler.Handle(new IncreaseStockCommand(Guid.NewGuid(), 10, "restock"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task IncreaseStock_ZeroAmount_ReturnsIncreaseAmountInvalid()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        repo.Store.Add(MakeItem(productId));
        var uow = new FakeUnitOfWork();

        var handler = new IncreaseStockCommandHandler(repo, uow, NullLogger<IncreaseStockCommandHandler>.Instance);
        var result = await handler.Handle(new IncreaseStockCommand(productId, 0, "restock"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task ReduceStock_ExistingItem_ReducesQuantityAndSaves()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 50));

        var handler = new ReduceStockCommandHandler(repo, uow, NullLogger<ReduceStockCommandHandler>.Instance);
        var result = await handler.Handle(new ReduceStockCommand(productId, 20, "sale"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(30, result.GetDataOrThrow().NewQuantity);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task ReduceStock_InsufficientStock_ReturnsFailureWithoutSaving()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 5));

        var handler = new ReduceStockCommandHandler(repo, uow, NullLogger<ReduceStockCommandHandler>.Instance);
        var result = await handler.Handle(new ReduceStockCommand(productId, 10, "sale"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.InsufficientStock.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task ReduceStock_UnknownProduct_ReturnsInventoryItemNotFound()
    {
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();

        var handler = new ReduceStockCommandHandler(repo, uow, NullLogger<ReduceStockCommandHandler>.Instance);
        var result = await handler.Handle(new ReduceStockCommand(Guid.NewGuid(), 5, "sale"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryApplicationErrors.InventoryItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task AdjustStock_ExistingItem_SetsQuantityToNewValue()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new AdjustStockCommandHandler(repo, uow, NullLogger<AdjustStockCommandHandler>.Instance);
        var result = await handler.Handle(new AdjustStockCommand(productId, 100, "inventory_count"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.GetDataOrThrow().NewQuantity);
        Assert.AreEqual(70, result.GetDataOrThrow().QuantityChanged);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task AdjustStock_ToZero_Succeeds()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new AdjustStockCommandHandler(repo, uow, NullLogger<AdjustStockCommandHandler>.Instance);
        var result = await handler.Handle(new AdjustStockCommand(productId, 0, "write-off"), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().NewQuantity);
    }

    [TestMethod]
    public async Task AdjustStock_NegativeQuantity_ReturnsFailureWithoutSaving()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeInventoryItemRepository();
        var uow = new FakeUnitOfWork();
        repo.Store.Add(MakeItem(productId, quantity: 30));

        var handler = new AdjustStockCommandHandler(repo, uow, NullLogger<AdjustStockCommandHandler>.Instance);
        var result = await handler.Handle(new AdjustStockCommand(productId, -1, "error"), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(0, uow.SaveCount);
    }
}