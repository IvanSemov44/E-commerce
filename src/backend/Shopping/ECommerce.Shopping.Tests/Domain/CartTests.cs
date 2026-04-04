using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Errors;
using ECommerce.Shopping.Domain.Events;

namespace ECommerce.Shopping.Tests.Domain;

[TestClass]
public class CartTests
{
    private static readonly Guid _testUserId   = Guid.NewGuid();
    private static readonly Guid _testProduct1 = Guid.NewGuid();
    private static readonly Guid _testProduct2 = Guid.NewGuid();

    [TestMethod]
    public void Create_Succeeds_WithUserId()
    {
        var cart = Cart.Create(_testUserId);

        Assert.AreEqual(_testUserId, cart.UserId);
        Assert.IsTrue(cart.IsEmpty);
        Assert.AreEqual(0, cart.ItemCount);
    }

    [TestMethod]
    public void AddItem_NewProduct_AddsCartItemAndRaisesEvent()
    {
        var cart = Cart.Create(_testUserId);

        var result = cart.AddItem(_testProduct1, 2, 9.99m, "USD");

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, cart.Items);
        Assert.AreEqual(2, cart.Items.First().Quantity);
        Assert.AreEqual(9.99m, cart.Items.First().UnitPrice);

        var events = cart.DomainEvents.OfType<ItemAddedToCartEvent>().ToList();
        Assert.HasCount(1, events);
    }

    [TestMethod]
    public void AddItem_ZeroQuantity_ReturnsQuantityInvalid()
    {
        var cart = Cart.Create(_testUserId);

        var result = cart.AddItem(_testProduct1, 0, 9.99m, "USD");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.QuantityInvalid, result.GetErrorOrThrow());
    }

    [TestMethod]
    public void AddItem_ExistingProduct_IncreasesQuantity()
    {
        var cart = Cart.Create(_testUserId);
        cart.AddItem(_testProduct1, 2, 9.99m, "USD");

        var result = cart.AddItem(_testProduct1, 3, 9.99m, "USD");

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, cart.Items);
        Assert.AreEqual(5, cart.Items.First().Quantity);
    }

    [TestMethod]
    public void AddItem_Exceeds50Items_ReturnsCartFull()
    {
        var cart = Cart.Create(_testUserId);
        for (int i = 0; i < 50; i++)
        {
            cart.AddItem(Guid.NewGuid(), 1, 9.99m, "USD");
        }

        var result = cart.AddItem(Guid.NewGuid(), 1, 9.99m, "USD");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.CartFull, result.GetErrorOrThrow());
    }

    [TestMethod]
    public void UpdateItemQuantity_ValidItem_Succeeds()
    {
        var cart = Cart.Create(_testUserId);
        cart.AddItem(_testProduct1, 2, 9.99m, "USD");
        var itemId = cart.Items.First().Id;

        var result = cart.UpdateItemQuantity(itemId, 5);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(5, cart.Items.First().Quantity);
    }

    [TestMethod]
    public void UpdateItemQuantity_InvalidItem_ReturnsCartItemNotFound()
    {
        var cart = Cart.Create(_testUserId);

        var result = cart.UpdateItemQuantity(Guid.NewGuid(), 5);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.CartItemNotFound, result.GetErrorOrThrow());
    }

    [TestMethod]
    public void RemoveItem_ValidItem_Removes()
    {
        var cart = Cart.Create(_testUserId);
        cart.AddItem(_testProduct1, 2, 9.99m, "USD");
        var itemId = cart.Items.First().Id;

        var result = cart.RemoveItem(itemId);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(cart.Items);
    }

    [TestMethod]
    public void Clear_RemovesAllItems()
    {
        var cart = Cart.Create(_testUserId);
        cart.AddItem(_testProduct1, 2, 9.99m, "USD");
        cart.AddItem(_testProduct2, 1, 4.99m, "USD");

        cart.Clear();

        Assert.IsEmpty(cart.Items);
    }

    [TestMethod]
    public void Subtotal_CalculatesCorrectly()
    {
        var cart = Cart.Create(_testUserId);
        cart.AddItem(_testProduct1, 2, 10.00m, "USD");
        cart.AddItem(_testProduct2, 3, 5.00m, "USD");

        Assert.AreEqual(35.00m, cart.Subtotal);
    }
}
