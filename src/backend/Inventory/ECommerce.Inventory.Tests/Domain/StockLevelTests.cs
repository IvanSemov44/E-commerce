using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Inventory.Domain.Errors;
using ECommerce.Inventory.Domain.ValueObjects;

namespace ECommerce.Inventory.Tests.Domain;

[TestClass]
public class StockLevelTests
{
    [TestMethod]
    public void Create_ZeroQuantity_Succeeds()
    {
        var result = StockLevel.Create(0);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public void Create_PositiveQuantity_Succeeds()
    {
        var result = StockLevel.Create(100);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public void Create_NegativeQuantity_ReturnsFailure()
    {
        var result = StockLevel.Create(-1);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.StockNegative.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Zero_Property_HasZeroQuantity()
    {
        Assert.AreEqual(0, StockLevel.Zero.Quantity);
    }

    [TestMethod]
    public void Reduce_ValidAmount_ReturnsNewInstanceWithReducedQuantity()
    {
        var stock = StockLevel.Create(50).GetDataOrThrow();

        var result = stock.Reduce(20);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(30, result.GetDataOrThrow().Quantity);
        Assert.AreEqual(50, stock.Quantity);
    }

    [TestMethod]
    public void Reduce_ExactStock_ReturnsZero()
    {
        var stock = StockLevel.Create(10).GetDataOrThrow();

        var result = stock.Reduce(10);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Quantity);
    }

    [TestMethod]
    public void Reduce_MoreThanStock_ReturnsInsufficientStock()
    {
        var stock = StockLevel.Create(5).GetDataOrThrow();

        var result = stock.Reduce(6);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.InsufficientStock.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Reduce_ZeroAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(50).GetDataOrThrow();

        var result = stock.Reduce(0);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.ReduceAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Reduce_NegativeAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(50).GetDataOrThrow();

        var result = stock.Reduce(-5);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.ReduceAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Increase_ValidAmount_ReturnsNewInstanceWithIncreasedQuantity()
    {
        var stock = StockLevel.Create(30).GetDataOrThrow();

        var result = stock.Increase(20);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, result.GetDataOrThrow().Quantity);
        Assert.AreEqual(30, stock.Quantity);
    }

    [TestMethod]
    public void Increase_ZeroAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(30).GetDataOrThrow();

        var result = stock.Increase(0);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Increase_NegativeAmount_ReturnsFailure()
    {
        var stock = StockLevel.Create(30).GetDataOrThrow();

        var result = stock.Increase(-10);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InventoryErrors.IncreaseAmountInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void TwoStockLevels_SameQuantity_AreEqual()
    {
        var a = StockLevel.Create(42).GetDataOrThrow();
        var b = StockLevel.Create(42).GetDataOrThrow();

        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void TwoStockLevels_DifferentQuantity_AreNotEqual()
    {
        var a = StockLevel.Create(42).GetDataOrThrow();
        var b = StockLevel.Create(43).GetDataOrThrow();

        Assert.AreNotEqual(a, b);
    }
}