# Phase 4, Step 6: Shopping Handler Unit Tests

**Prerequisite**: Step 2 (`ECommerce.Shopping.Application`) is complete and builds. `ECommerce.Shopping.Tests` project from step 5 exists.

---

## Task: Add Handler Tests to ECommerce.Shopping.Tests

Files go in `Shopping/ECommerce.Shopping.Tests/Application/`.

---

### File: `Shopping/ECommerce.Shopping.Tests/Application/Fakes.cs`

```csharp
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Shopping.Tests.Application;

sealed class FakeCartRepository : ICartRepository
{
    public List<Cart> Store = new();

    public Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(c => c.UserId == userId));

    public Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(c => c.Id == cartId));

    public Task UpsertAsync(Cart cart, CancellationToken ct = default)
    {
        Store.RemoveAll(c => c.Id == cart.Id);
        Store.Add(cart);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Cart cart, CancellationToken ct = default)
    {
        Store.RemoveAll(c => c.Id == cart.Id);
        return Task.CompletedTask;
    }
}

sealed class FakeWishlistRepository : IWishlistRepository
{
    public List<Wishlist> Store = new();

    public Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(w => w.UserId == userId));

    public Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        Store.RemoveAll(w => w.UserId == wishlist.UserId);
        Store.Add(wishlist);
        return Task.CompletedTask;
    }
}

sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

sealed class FakeDbReader : IShoppingDbReader
{
    public Dictionary<Guid, ProductPriceInfo> Products = new();
    public HashSet<Guid> InStockProducts = new();

    public Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct)
        => Task.FromResult(Products.TryGetValue(productId, out var p) ? p : null);

    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct)
        => Task.FromResult(Products.ContainsKey(productId));

    public Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct)
        => Task.FromResult(InStockProducts.Contains(productId));
}
```

---

### File: `Shopping/ECommerce.Shopping.Tests/Application/CartCommandHandlerTests.cs`

```csharp
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.Commands.ClearCart;
using ECommerce.Shopping.Application.Commands.RemoveFromCart;
using ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Errors;

namespace ECommerce.Shopping.Tests.Application;

[TestClass]
public class CartCommandHandlerTests
{
    private static readonly Guid UserId    = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    // ── AddToCartCommandHandler ───────────────────────────────────────────────

    [TestMethod]
    public async Task AddToCart_ProductExists_CreatesCartAndAddsItem()
    {
        var repo = new FakeCartRepository();
        var db   = new FakeDbReader();
        db.Products[ProductId] = new ProductPriceInfo(9.99m, "USD");
        var uow = new FakeUnitOfWork();

        var handler = new AddToCartCommandHandler(repo, db, uow);
        var result  = await handler.Handle(new AddToCartCommand(UserId, ProductId, 2), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.GetDataOrThrow().Items.Count);
        Assert.AreEqual(2, result.GetDataOrThrow().Items[0].Quantity);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task AddToCart_ProductNotFound_ReturnsFailure()
    {
        var repo = new FakeCartRepository();
        var db   = new FakeDbReader(); // empty — no products
        var uow  = new FakeUnitOfWork();

        var handler = new AddToCartCommandHandler(repo, db, uow);
        var result  = await handler.Handle(new AddToCartCommand(UserId, ProductId, 1), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingApplicationErrors.ProductNotFound.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task AddToCart_SameProductTwice_IncreasesQuantity()
    {
        var repo = new FakeCartRepository();
        var db   = new FakeDbReader();
        db.Products[ProductId] = new ProductPriceInfo(9.99m, "USD");
        var uow = new FakeUnitOfWork();

        var handler = new AddToCartCommandHandler(repo, db, uow);
        await handler.Handle(new AddToCartCommand(UserId, ProductId, 2), default);
        var result = await handler.Handle(new AddToCartCommand(UserId, ProductId, 3), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.GetDataOrThrow().Items.Count, "Must not duplicate");
        Assert.AreEqual(5, result.GetDataOrThrow().Items[0].Quantity, "2 + 3 = 5");
    }

    [TestMethod]
    public async Task AddToCart_ExistingCartLoaded_UsedInsteadOfCreatingNew()
    {
        // Pre-populate a cart for the user
        var existingCart = Cart.Create(UserId);
        var repo = new FakeCartRepository();
        repo.Store.Add(existingCart);

        var db  = new FakeDbReader();
        db.Products[ProductId] = new ProductPriceInfo(5m, "USD");
        var uow = new FakeUnitOfWork();

        var handler = new AddToCartCommandHandler(repo, db, uow);
        var result  = await handler.Handle(new AddToCartCommand(UserId, ProductId, 1), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(existingCart.Id, result.GetDataOrThrow().Id, "Must reuse existing cart");
        Assert.AreEqual(1, repo.Store.Count, "Must not create a second cart");
    }

    // ── RemoveFromCartCommandHandler ──────────────────────────────────────────

    [TestMethod]
    public async Task RemoveFromCart_ExistingItem_RemovesIt()
    {
        var cart = Cart.Create(UserId);
        cart.AddItem(ProductId, 1, 5m, "USD");
        var itemId = cart.Items.First().Id;

        var repo = new FakeCartRepository();
        repo.Store.Add(cart);
        var uow = new FakeUnitOfWork();

        var handler = new RemoveFromCartCommandHandler(repo, uow);
        var result  = await handler.Handle(new RemoveFromCartCommand(UserId, itemId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Items.Count);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task RemoveFromCart_NoCartForUser_ReturnsCartNotFound()
    {
        var repo = new FakeCartRepository(); // empty
        var uow  = new FakeUnitOfWork();

        var handler = new RemoveFromCartCommandHandler(repo, uow);
        var result  = await handler.Handle(new RemoveFromCartCommand(UserId, Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingApplicationErrors.CartNotFound.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task RemoveFromCart_UnknownItemId_ReturnsCartItemNotFound()
    {
        var cart = Cart.Create(UserId);
        var repo = new FakeCartRepository();
        repo.Store.Add(cart);
        var uow = new FakeUnitOfWork();

        var handler = new RemoveFromCartCommandHandler(repo, uow);
        var result  = await handler.Handle(new RemoveFromCartCommand(UserId, Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.CartItemNotFound.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    // ── UpdateCartItemQuantityCommandHandler ──────────────────────────────────

    [TestMethod]
    public async Task UpdateCartItemQuantity_ValidItem_UpdatesQuantity()
    {
        var cart = Cart.Create(UserId);
        cart.AddItem(ProductId, 1, 5m, "USD");
        var itemId = cart.Items.First().Id;

        var repo = new FakeCartRepository();
        repo.Store.Add(cart);
        var uow = new FakeUnitOfWork();

        var handler = new UpdateCartItemQuantityCommandHandler(repo, uow);
        var result  = await handler.Handle(
            new UpdateCartItemQuantityCommand(UserId, itemId, 10), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(10, result.GetDataOrThrow().Items[0].Quantity);
        Assert.AreEqual(1, uow.SaveCount);
    }

    // ── ClearCartCommandHandler ───────────────────────────────────────────────

    [TestMethod]
    public async Task ClearCart_WithUserId_ClearsAndSaves()
    {
        var cart = Cart.Create(UserId);
        cart.AddItem(ProductId, 2, 5m, "USD");
        var repo = new FakeCartRepository();
        repo.Store.Add(cart);
        var uow = new FakeUnitOfWork();

        var handler = new ClearCartCommandHandler(repo, uow);
        var result  = await handler.Handle(new ClearCartCommand(UserId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Items.Count);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task ClearCart_NullUserId_ReturnsEmptyCartWithoutSaving()
    {
        var repo = new FakeCartRepository();
        var uow  = new FakeUnitOfWork();

        var handler = new ClearCartCommandHandler(repo, uow);
        var result  = await handler.Handle(new ClearCartCommand(null), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().Items.Count);
        Assert.AreEqual(0, uow.SaveCount, "Anonymous clear must NOT touch the DB");
    }
}
```

---

### File: `Shopping/ECommerce.Shopping.Tests/Application/WishlistCommandHandlerTests.cs`

```csharp
using ECommerce.Shopping.Application.Commands.AddToWishlist;
using ECommerce.Shopping.Application.Commands.ClearWishlist;
using ECommerce.Shopping.Application.Commands.RemoveFromWishlist;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Application.Queries.GetWishlist;
using ECommerce.Shopping.Application.Queries.IsProductInWishlist;
using ECommerce.Shopping.Domain.Errors;

namespace ECommerce.Shopping.Tests.Application;

[TestClass]
public class WishlistHandlerTests
{
    private static readonly Guid UserId    = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    // ── AddToWishlistCommandHandler ───────────────────────────────────────────

    [TestMethod]
    public async Task AddToWishlist_ProductExists_AddsAndSaves()
    {
        var repo = new FakeWishlistRepository();
        var db   = new FakeDbReader();
        db.Products[ProductId] = new ProductPriceInfo(0m, "USD"); // price doesn't matter
        var uow = new FakeUnitOfWork();

        var handler = new AddToWishlistCommandHandler(repo, db, uow);
        var result  = await handler.Handle(new AddToWishlistCommand(UserId, ProductId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.GetDataOrThrow().ProductIds.Contains(ProductId));
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task AddToWishlist_ProductNotFound_ReturnsFailure()
    {
        var repo = new FakeWishlistRepository();
        var db   = new FakeDbReader(); // empty
        var uow  = new FakeUnitOfWork();

        var handler = new AddToWishlistCommandHandler(repo, db, uow);
        var result  = await handler.Handle(new AddToWishlistCommand(UserId, ProductId), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingApplicationErrors.ProductNotFound.Code, result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task AddToWishlist_AlreadyPresent_IsIdempotent()
    {
        var repo = new FakeWishlistRepository();
        var db   = new FakeDbReader();
        db.Products[ProductId] = new ProductPriceInfo(0m, "USD");
        var uow = new FakeUnitOfWork();

        var handler = new AddToWishlistCommandHandler(repo, db, uow);
        await handler.Handle(new AddToWishlistCommand(UserId, ProductId), default);
        var result = await handler.Handle(new AddToWishlistCommand(UserId, ProductId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.GetDataOrThrow().ProductIds.Count, "Must not duplicate");
    }

    // ── RemoveFromWishlistCommandHandler ──────────────────────────────────────

    [TestMethod]
    public async Task RemoveFromWishlist_AbsentProduct_IsNoOp()
    {
        // No wishlist for this user — remove is still a success
        var repo = new FakeWishlistRepository();
        var uow  = new FakeUnitOfWork();

        var handler = new RemoveFromWishlistCommandHandler(repo, uow);
        var result  = await handler.Handle(
            new RemoveFromWishlistCommand(UserId, Guid.NewGuid()), default);

        Assert.IsTrue(result.IsSuccess, "Remove of absent product must succeed (no-op)");
        Assert.AreEqual(1, uow.SaveCount);
    }

    // ── IsProductInWishlistQueryHandler ───────────────────────────────────────

    [TestMethod]
    public async Task IsProductInWishlist_Present_ReturnsTrue()
    {
        var repo = new FakeWishlistRepository();
        var db   = new FakeDbReader();
        db.Products[ProductId] = new ProductPriceInfo(0m, "USD");
        var uow = new FakeUnitOfWork();

        // Add to wishlist first
        var addHandler = new AddToWishlistCommandHandler(repo, db, uow);
        await addHandler.Handle(new AddToWishlistCommand(UserId, ProductId), default);

        var queryHandler = new IsProductInWishlistQueryHandler(repo);
        var result = await queryHandler.Handle(
            new IsProductInWishlistQuery(UserId, ProductId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.GetDataOrThrow());
    }

    [TestMethod]
    public async Task IsProductInWishlist_Absent_ReturnsFalse()
    {
        var repo    = new FakeWishlistRepository(); // empty
        var handler = new IsProductInWishlistQueryHandler(repo);

        var result = await handler.Handle(
            new IsProductInWishlistQuery(UserId, Guid.NewGuid()), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.GetDataOrThrow());
    }

    // ── GetWishlistQueryHandler ───────────────────────────────────────────────

    [TestMethod]
    public async Task GetWishlist_NoExistingWishlist_ReturnsEmptyDto()
    {
        var repo    = new FakeWishlistRepository();
        var handler = new GetWishlistQueryHandler(repo);

        var result = await handler.Handle(new GetWishlistQuery(UserId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().ProductIds.Count);
    }

    // ── ClearWishlistCommandHandler ───────────────────────────────────────────

    [TestMethod]
    public async Task ClearWishlist_ExistingWishlist_ClearsAndSaves()
    {
        var repo = new FakeWishlistRepository();
        var db   = new FakeDbReader();
        db.Products[ProductId] = new ProductPriceInfo(0m, "USD");
        var uow = new FakeUnitOfWork();

        var addHandler = new AddToWishlistCommandHandler(repo, db, uow);
        await addHandler.Handle(new AddToWishlistCommand(UserId, ProductId), default);
        uow = new FakeUnitOfWork(); // reset save count

        var clearHandler = new ClearWishlistCommandHandler(repo, uow);
        var result = await clearHandler.Handle(new ClearWishlistCommand(UserId), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.GetDataOrThrow().ProductIds.Count);
        Assert.AreEqual(1, uow.SaveCount);
    }
}
```

### 3. Run handler tests

```bash
cd src/backend
dotnet test Shopping/ECommerce.Shopping.Tests/ECommerce.Shopping.Tests.csproj \
    --filter "FullyQualifiedName~CartCommandHandlerTests|FullyQualifiedName~WishlistHandlerTests"
```

---

## Acceptance Criteria

- [ ] `Fakes.cs` — `FakeCartRepository`, `FakeWishlistRepository`, `FakeUnitOfWork`, `FakeDbReader`
- [ ] `CartCommandHandlerTests.cs` — all tests pass
  - AddToCart: product exists (creates cart), product not found (no save), same product twice (qty increase), existing cart reused
  - RemoveFromCart: success, no cart → CartNotFound, unknown item → CartItemNotFound
  - UpdateCartItemQuantity: success
  - ClearCart: with userId (saves), null userId (no save — anonymous)
- [ ] `WishlistHandlerTests.cs` — all tests pass
  - AddToWishlist: product exists, product not found (no save), idempotent (no duplicate)
  - RemoveFromWishlist: absent product is a no-op (success)
  - IsProductInWishlist: true/false
  - GetWishlist: empty when no wishlist exists
  - ClearWishlist: clears and saves
- [ ] `SaveChangesAsync` never called on failure
- [ ] All tests are fast (no I/O, no real DB)
