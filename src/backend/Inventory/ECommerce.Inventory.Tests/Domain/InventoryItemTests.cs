using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.Errors;
using ECommerce.Inventory.Domain.Events;

namespace ECommerce.Inventory.Tests.Domain;

[TestClass]
public class InventoryItemTests
{
    private static readonly Guid _testProductId = Guid.NewGuid();

    [TestMethod]
    public void Create_ValidInputs_Succeeds()
    {
        var result = InventoryItem.Create(_testProductId, 100, 10);

        Assert.IsTrue(result.IsSuccess);
        var item = result.GetDataOrThrow();
        Assert.AreEqual(_testProductId, item.ProductId);
        Assert.AreEqual(100, item.Stock.Quantity);
        Assert.AreEqual(10, item.LowStockThreshold);
        Assert.IsTrue(item.TrackInventory);
    }

    [TestMethod]
    public void Create_ZeroInitialQuantity_Succeeds()
    {
        var result = InventoryItem.Create(_testProductId, 0, 5);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Create_NegativeInitialQuantity_ReturnsFailure()
    {
        var result = InventoryItem.Create(_testProductId, -1, 5);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.StockNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_NegativeThreshold_ReturnsFailure()
    {
        var result = InventoryItem.Create(_testProductId, 50, -1);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.ThresholdNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_ZeroThreshold_Succeeds()
    {
        var result = InventoryItem.Create(_testProductId, 10, 0);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Reduce_ValidAmount_UpdatesStockAndAddsLog()
    {
        var item = InventoryItem.Create(_testProductId, 50, 10).GetDataOrThrow();

        var result = item.Reduce(20, "sale");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(30, item.Stock.Quantity);
        Assert.HasCount(1, item.Log);
        Assert.AreEqual(-20, item.Log.First().Delta);
        Assert.AreEqual("sale", item.Log.First().Reason);
    }

    [TestMethod]
    public void Reduce_InsufficientStock_ReturnsFailure()
    {
        var item = InventoryItem.Create(_testProductId, 5, 10).GetDataOrThrow();

        var result = item.Reduce(10, "sale");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.InsufficientStock.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(5, item.Stock.Quantity);
    }

    [TestMethod]
    public void Reduce_RaisesStockReducedEvent()
    {
        var item = InventoryItem.Create(_testProductId, 50, 10).GetDataOrThrow();

        item.Reduce(20, "sale");

        var events = item.DomainEvents.OfType<StockReducedEvent>().ToList();
        Assert.HasCount(1, events);
        Assert.AreEqual(20, events[0].QuantityReduced);
        Assert.AreEqual(30, events[0].NewQuantity);
        Assert.AreEqual("sale", events[0].Reason);
    }

    [TestMethod]
    public void Reduce_CrossingThresholdFromAbove_RaisesLowStockDetectedEvent()
    {
        var item = InventoryItem.Create(_testProductId, 20, 10).GetDataOrThrow();

        item.Reduce(15, "sale");

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.HasCount(1, events);
        Assert.AreEqual(5, events[0].CurrentStock);
        Assert.AreEqual(10, events[0].Threshold);
    }

    [TestMethod]
    public void Reduce_AlreadyBelowThreshold_DoesNotRaiseLowStockDetectedEvent()
    {
        var item = InventoryItem.Create(_testProductId, 5, 10).GetDataOrThrow();
        item.ClearDomainEvents();

        item.Reduce(2, "sale");

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.IsEmpty(events);
    }

    [TestMethod]
    public void Reduce_StockExactlyAtThreshold_DoesNotRaiseLowStockDetectedEvent()
    {
        var item = InventoryItem.Create(_testProductId, 10, 10).GetDataOrThrow();
        item.ClearDomainEvents();

        item.Reduce(1, "sale");

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.IsEmpty(events);
    }

    [TestMethod]
    public void Reduce_ToExactlyThreshold_RaisesLowStockDetectedEvent()
    {
        var item = InventoryItem.Create(_testProductId, 15, 10).GetDataOrThrow();
        item.ClearDomainEvents();

        item.Reduce(5, "sale");

        var events = item.DomainEvents.OfType<LowStockDetectedEvent>().ToList();
        Assert.HasCount(1, events);
    }

    [TestMethod]
    public void Increase_ValidAmount_UpdatesStockAndAddsLog()
    {
        var item = InventoryItem.Create(_testProductId, 30, 10).GetDataOrThrow();

        var result = item.Increase(20, "restock");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, item.Stock.Quantity);
        Assert.HasCount(1, item.Log);
        Assert.AreEqual(20, item.Log.First().Delta);
    }

    [TestMethod]
    public void Increase_RaisesStockReplenishedEvent()
    {
        var item = InventoryItem.Create(_testProductId, 30, 10).GetDataOrThrow();

        item.Increase(20, "restock");

        var events = item.DomainEvents.OfType<StockReplenishedEvent>().ToList();
        Assert.HasCount(1, events);
        Assert.AreEqual(20, events[0].QuantityAdded);
        Assert.AreEqual(50, events[0].NewQuantity);
    }

    [TestMethod]
    public void Increase_ZeroAmount_ReturnsFailure()
    {
        var item = InventoryItem.Create(_testProductId, 30, 10).GetDataOrThrow();

        var result = item.Increase(0, "restock");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Adjust_ValidQuantity_SetsStockAndAddsLog()
    {
        var item = InventoryItem.Create(_testProductId, 30, 10).GetDataOrThrow();

        var result = item.Adjust(100, "inventory_count");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, item.Stock.Quantity);
        Assert.HasCount(1, item.Log);
        Assert.AreEqual(70, item.Log.First().Delta);
    }

    [TestMethod]
    public void Adjust_ToZero_Succeeds()
    {
        var item = InventoryItem.Create(_testProductId, 30, 10).GetDataOrThrow();

        var result = item.Adjust(0, "write-off");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, item.Stock.Quantity);
    }

    [TestMethod]
    public void Adjust_NegativeQuantity_ReturnsFailure()
    {
        var item = InventoryItem.Create(_testProductId, 30, 10).GetDataOrThrow();

        var result = item.Adjust(-5, "error");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.StockNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Adjust_DoesNotRaiseDomainEvents()
    {
        var item = InventoryItem.Create(_testProductId, 30, 10).GetDataOrThrow();
        item.ClearDomainEvents();

        item.Adjust(100, "correction");

        Assert.IsEmpty(item.DomainEvents);
    }

    [TestMethod]
    public void MultipleOperations_LogEntriesAccumulate()
    {
        var item = InventoryItem.Create(_testProductId, 100, 10).GetDataOrThrow();

        item.Reduce(20, "sale");
        item.Increase(50, "restock");
        item.Adjust(80, "correction");

        Assert.HasCount(3, item.Log);
    }
}
