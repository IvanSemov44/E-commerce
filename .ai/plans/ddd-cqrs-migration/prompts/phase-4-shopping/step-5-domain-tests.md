# Phase 4, Step 5: Shopping Domain Unit Tests

**Prerequisite**: Step 1 (`ECommerce.Shopping.Domain`) is complete and builds.

---

## Task: Create ECommerce.Shopping.Tests Project

### 1. Create the test project

```bash
cd src/backend
dotnet new mstest -n ECommerce.Shopping.Tests -f net10.0 -o Shopping/ECommerce.Shopping.Tests
dotnet sln ../../ECommerce.sln add Shopping/ECommerce.Shopping.Tests/ECommerce.Shopping.Tests.csproj

dotnet add Shopping/ECommerce.Shopping.Tests/ECommerce.Shopping.Tests.csproj \
    reference Shopping/ECommerce.Shopping.Domain/ECommerce.Shopping.Domain.csproj
dotnet add Shopping/ECommerce.Shopping.Tests/ECommerce.Shopping.Tests.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

rm Shopping/ECommerce.Shopping.Tests/UnitTest1.cs
```

---

### File: `Shopping/ECommerce.Shopping.Tests/Domain/CartTests.cs`

```csharp
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Errors;
using ECommerce.Shopping.Domain.Events;

namespace ECommerce.Shopping.Tests.Domain;

[TestClass]
public class CartTests
{
    private static readonly Guid TestUserId   = Guid.NewGuid();
    private static readonly Guid TestProduct1 = Guid.NewGuid();
    private static readonly Guid TestProduct2 = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_Succeeds_WithUserId()
    {
        var cart = Cart.Create(TestUserId);

        Assert.AreEqual(TestUserId, cart.UserId);
        Assert.IsTrue(cart.IsEmpty);
        Assert.AreEqual(0, cart.ItemCount);
    }

    // ── AddItem — basic ───────────────────────────────────────────────────────

    [TestMethod]
    public void AddItem_NewProduct_AddsCartItemAndRaisesEvent()
    {
        var cart = Cart.Create(TestUserId);

        var result = cart.AddItem(TestProduct1, 2, 9.99m, "USD");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, cart.Items.Count);
        Assert.AreEqual(2, cart.Items.First().Quantity);
        Assert.AreEqual(9.99m, cart.Items.First().UnitPrice);

        var events = cart.DomainEvents.OfType<ItemAddedToCartEvent>().ToList();
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(TestProduct1, events[0].ProductId);
        Assert.AreEqual(2, events[0].Quantity);
    }

    [TestMethod]
    public void AddItem_ZeroQuantity_ReturnsQuantityInvalid()
    {
        var cart = Cart.Create(TestUserId);

        var result = cart.AddItem(TestProduct1, 0, 9.99m, "USD");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.QuantityInvalid.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, cart.Items.Count);
    }

    [TestMethod]
    public void AddItem_NegativeQuantity_ReturnsQuantityInvalid()
    {
        var cart = Cart.Create(TestUserId);

        var result = cart.AddItem(TestProduct1, -1, 9.99m, "USD");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.QuantityInvalid.Code, result.GetErrorOrThrow().Code);
    }

    // ── AddItem — idempotency ─────────────────────────────────────────────────

    [TestMethod]
    public void AddItem_SameProductTwice_IncreasesQuantityNotDuplicates()
    {
        var cart = Cart.Create(TestUserId);
        cart.AddItem(TestProduct1, 2, 9.99m, "USD");
        cart.ClearDomainEvents();

        var result = cart.AddItem(TestProduct1, 3, 9.99m, "USD");  // add same product again

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, cart.Items.Count,   "Must NOT add a duplicate CartItem");
        Assert.AreEqual(5, cart.Items.First().Quantity, "Quantity must be 2 + 3 = 5");

        // Raises CartItemQuantityUpdated (not ItemAdded) for idempotent add
        var updateEvents = cart.DomainEvents.OfType<CartItemQuantityUpdatedEvent>().ToList();
        Assert.AreEqual(1, updateEvents.Count);
        Assert.AreEqual(5, updateEvents[0].NewQuantity);
    }

    [TestMethod]
    public void AddItem_DifferentProducts_AllowsMultipleItems()
    {
        var cart = Cart.Create(TestUserId);

        cart.AddItem(TestProduct1, 1, 10m, "USD");
        cart.AddItem(TestProduct2, 2, 20m, "USD");

        Assert.AreEqual(2, cart.Items.Count);
        Assert.AreEqual(3, cart.ItemCount);  // 1 + 2
    }

    // ── AddItem — CartFull limit ──────────────────────────────────────────────

    [TestMethod]
    public void AddItem_ExceedsDistinctLimit_ReturnsCartFull()
    {
        var cart = Cart.Create(TestUserId);

        // Fill cart with 50 distinct products
        for (int i = 0; i < 50; i++)
            cart.AddItem(Guid.NewGuid(), 1, 1m, "USD");

        // 51st distinct product must fail
        var result = cart.AddItem(Guid.NewGuid(), 1, 1m, "USD");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.CartFull.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddItem_ExceedsLimit_ButSameProduct_Succeeds()
    {
        var cart = Cart.Create(TestUserId);
        for (int i = 0; i < 50; i++)
            cart.AddItem(Guid.NewGuid(), 1, 1m, "USD");

        // Same product — idempotent increase, NOT a new item, must NOT hit CartFull
        var firstProductId = cart.Items.First().ProductId;
        var result = cart.AddItem(firstProductId, 5, 1m, "USD");

        Assert.IsTrue(result.IsSuccess, "Adding to existing item must succeed even at the 50-item limit");
    }

    // ── UnitPrice snapshot ────────────────────────────────────────────────────

    [TestMethod]
    public void AddItem_SnapshotPrice_IsStoredAtAddTime()
    {
        var cart = Cart.Create(TestUserId);

        cart.AddItem(TestProduct1, 1, 49.99m, "USD");

        // Price is snapshotted — reading it back must return exactly what was given
        Assert.AreEqual(49.99m, cart.Items.First().UnitPrice);
        Assert.AreEqual("USD", cart.Items.First().Currency);
    }

    [TestMethod]
    public void Subtotal_IsCalculatedFromSnapshotPrices()
    {
        var cart = Cart.Create(TestUserId);
        cart.AddItem(TestProduct1, 3, 10m, "USD");
        cart.AddItem(TestProduct2, 2, 25m, "USD");

        // 3 * 10 + 2 * 25 = 30 + 50 = 80
        Assert.AreEqual(80m, cart.Subtotal);
    }

    // ── UpdateItemQuantity ────────────────────────────────────────────────────

    [TestMethod]
    public void UpdateItemQuantity_ExistingItem_ChangesQuantity()
    {
        var cart = Cart.Create(TestUserId);
        cart.AddItem(TestProduct1, 2, 9.99m, "USD");
        var itemId = cart.Items.First().Id;
        cart.ClearDomainEvents();

        var result = cart.UpdateItemQuantity(itemId, 5);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(5, cart.Items.First().Quantity);

        var events = cart.DomainEvents.OfType<CartItemQuantityUpdatedEvent>().ToList();
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(5, events[0].NewQuantity);
    }

    [TestMethod]
    public void UpdateItemQuantity_ZeroQuantity_ReturnsQuantityInvalid()
    {
        var cart = Cart.Create(TestUserId);
        cart.AddItem(TestProduct1, 2, 9.99m, "USD");
        var itemId = cart.Items.First().Id;

        var result = cart.UpdateItemQuantity(itemId, 0);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.QuantityInvalid.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(2, cart.Items.First().Quantity, "Quantity must be unchanged on failure");
    }

    [TestMethod]
    public void UpdateItemQuantity_UnknownItemId_ReturnsCartItemNotFound()
    {
        var cart = Cart.Create(TestUserId);

        var result = cart.UpdateItemQuantity(Guid.NewGuid(), 3);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.CartItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── RemoveItem ────────────────────────────────────────────────────────────

    [TestMethod]
    public void RemoveItem_ExistingItem_RemovesIt()
    {
        var cart = Cart.Create(TestUserId);
        cart.AddItem(TestProduct1, 1, 9.99m, "USD");
        var itemId = cart.Items.First().Id;

        var result = cart.RemoveItem(itemId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, cart.Items.Count);
    }

    [TestMethod]
    public void RemoveItem_UnknownItemId_ReturnsCartItemNotFound()
    {
        var cart = Cart.Create(TestUserId);

        var result = cart.RemoveItem(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.CartItemNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Clear_RemovesAllItemsAndRaisesEvent()
    {
        var cart = Cart.Create(TestUserId);
        cart.AddItem(TestProduct1, 2, 9.99m, "USD");
        cart.AddItem(TestProduct2, 1, 19.99m, "USD");
        cart.ClearDomainEvents();

        cart.Clear();

        Assert.IsTrue(cart.IsEmpty);
        Assert.AreEqual(0, cart.ItemCount);

        var events = cart.DomainEvents.OfType<CartClearedEvent>().ToList();
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(TestUserId, events[0].UserId);
    }

    [TestMethod]
    public void Clear_AlreadyEmpty_RaisesEvent()
    {
        var cart = Cart.Create(TestUserId);
        cart.ClearDomainEvents();

        cart.Clear();

        var events = cart.DomainEvents.OfType<CartClearedEvent>().ToList();
        Assert.AreEqual(1, events.Count, "Clear on empty cart must still raise CartClearedEvent");
    }
}
```

---

### File: `Shopping/ECommerce.Shopping.Tests/Domain/WishlistTests.cs`

```csharp
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Errors;

namespace ECommerce.Shopping.Tests.Domain;

[TestClass]
public class WishlistTests
{
    private static readonly Guid TestUserId   = Guid.NewGuid();
    private static readonly Guid TestProduct1 = Guid.NewGuid();
    private static readonly Guid TestProduct2 = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_Succeeds_WithUserId()
    {
        var wishlist = Wishlist.Create(TestUserId);

        Assert.AreEqual(TestUserId, wishlist.UserId);
        Assert.AreEqual(0, wishlist.ProductIds.Count);
    }

    // ── AddProduct — idempotency ──────────────────────────────────────────────

    [TestMethod]
    public void AddProduct_NewProduct_AddsIt()
    {
        var wishlist = Wishlist.Create(TestUserId);

        var result = wishlist.AddProduct(TestProduct1);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, wishlist.ProductIds.Count);
        Assert.IsTrue(wishlist.Contains(TestProduct1));
    }

    [TestMethod]
    public void AddProduct_AlreadyPresent_IsNoOp()
    {
        var wishlist = Wishlist.Create(TestUserId);
        wishlist.AddProduct(TestProduct1);

        // Adding same product again must succeed and NOT create a duplicate
        var result = wishlist.AddProduct(TestProduct1);

        Assert.IsTrue(result.IsSuccess, "Idempotent add must return Ok");
        Assert.AreEqual(1, wishlist.ProductIds.Count, "Must NOT duplicate");
    }

    [TestMethod]
    public void AddProduct_MultipleDistinctProducts_AddsAll()
    {
        var wishlist = Wishlist.Create(TestUserId);

        wishlist.AddProduct(TestProduct1);
        wishlist.AddProduct(TestProduct2);

        Assert.AreEqual(2, wishlist.ProductIds.Count);
    }

    // ── WishlistFull limit ────────────────────────────────────────────────────

    [TestMethod]
    public void AddProduct_Exceeds100Limit_ReturnsWishlistFull()
    {
        var wishlist = Wishlist.Create(TestUserId);
        for (int i = 0; i < 100; i++)
            wishlist.AddProduct(Guid.NewGuid());

        var result = wishlist.AddProduct(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.WishlistFull.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddProduct_At100Limit_SameProduct_IsNoOp()
    {
        var wishlist = Wishlist.Create(TestUserId);
        var firstId = Guid.NewGuid();
        wishlist.AddProduct(firstId);
        for (int i = 1; i < 100; i++)
            wishlist.AddProduct(Guid.NewGuid());

        // Re-adding the first product must still be a no-op, not hit the limit
        var result = wishlist.AddProduct(firstId);

        Assert.IsTrue(result.IsSuccess, "Re-adding existing product at limit must succeed");
        Assert.AreEqual(100, wishlist.ProductIds.Count, "Count must stay at 100");
    }

    // ── RemoveProduct — no-op if absent ──────────────────────────────────────

    [TestMethod]
    public void RemoveProduct_ExistingProduct_RemovesIt()
    {
        var wishlist = Wishlist.Create(TestUserId);
        wishlist.AddProduct(TestProduct1);

        wishlist.RemoveProduct(TestProduct1);

        Assert.AreEqual(0, wishlist.ProductIds.Count);
        Assert.IsFalse(wishlist.Contains(TestProduct1));
    }

    [TestMethod]
    public void RemoveProduct_NotPresent_IsNoOp()
    {
        var wishlist = Wishlist.Create(TestUserId);

        // Must NOT throw — remove of absent product is a no-op
        wishlist.RemoveProduct(Guid.NewGuid());

        Assert.AreEqual(0, wishlist.ProductIds.Count);
    }

    // ── Contains ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Contains_PresentProduct_ReturnsTrue()
    {
        var wishlist = Wishlist.Create(TestUserId);
        wishlist.AddProduct(TestProduct1);

        Assert.IsTrue(wishlist.Contains(TestProduct1));
    }

    [TestMethod]
    public void Contains_AbsentProduct_ReturnsFalse()
    {
        var wishlist = Wishlist.Create(TestUserId);

        Assert.IsFalse(wishlist.Contains(TestProduct1));
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Clear_RemovesAllProducts()
    {
        var wishlist = Wishlist.Create(TestUserId);
        wishlist.AddProduct(TestProduct1);
        wishlist.AddProduct(TestProduct2);

        wishlist.Clear();

        Assert.AreEqual(0, wishlist.ProductIds.Count);
    }
}
```

### 3. Run domain tests

```bash
cd src/backend
dotnet test Shopping/ECommerce.Shopping.Tests/ECommerce.Shopping.Tests.csproj \
    --filter "FullyQualifiedName~CartTests|FullyQualifiedName~WishlistTests"
```

---

## Acceptance Criteria

- [ ] `CartTests.cs` — all tests pass
  - AddItem: basic, zero/negative quantity, idempotency (same product → qty increase, not duplicate), CartFull limit, limit + same product succeeds
  - UnitPrice snapshot confirmed
  - Subtotal calculation from snapshot prices
  - UpdateItemQuantity: success, zero qty → QuantityInvalid, unknown id → CartItemNotFound
  - RemoveItem: success, unknown id → CartItemNotFound
  - Clear: raises `CartClearedEvent`, clears on empty still raises event
- [ ] `WishlistTests.cs` — all tests pass
  - AddProduct: basic, idempotency (already present → no-op, no duplicate), WishlistFull limit, limit + same product → no-op
  - RemoveProduct: success, absent product → no-op (never fails)
  - Contains: true/false
  - Clear: removes all
- [ ] All tests are fast (no I/O, no database)
