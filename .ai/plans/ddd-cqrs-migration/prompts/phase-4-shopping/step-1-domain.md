# Phase 4, Step 1: Shopping Domain Project

**Prerequisite**: Phase 3 (Inventory) complete and all tests pass.

---

## Context

This step creates both aggregates in a single `ECommerce.Shopping.Domain` project. Cart and Wishlist are in the same bounded context — they share the same domain error class and the same project.

**New concepts in this phase:**
- `CartItem` is a **child entity with internal constructor** — only `Cart.AddItem()` creates it.
- `Cart.AddItem()` is **idempotent**: adding a product already in the cart increases quantity, does not duplicate.
- `CartItem.UnitPrice` is a **snapshot** — the price at add time. It does NOT update when the Catalog price changes.
- `Wishlist._productIds` is a `List<Guid>` — no child entity, because product IDs have no properties of their own.
- `Cart` carries a `RowVersion` concurrency token — the existing table has `[Timestamp] RowVersion` and it must be preserved.
- **Session carts are out of scope for Phase 4.** The new aggregate only supports user-owned carts (`Guid UserId`). Anonymous cart support via `SessionId` is deferred. See step-4 (cutover) for how the controller handles this.

---

## Task: Create ECommerce.Shopping.Domain Project

### 1. Create the project

```bash
cd src/backend
mkdir -p Shopping
dotnet new classlib -n ECommerce.Shopping.Domain -f net10.0 -o Shopping/ECommerce.Shopping.Domain
dotnet sln ../../ECommerce.sln add Shopping/ECommerce.Shopping.Domain/ECommerce.Shopping.Domain.csproj

dotnet add Shopping/ECommerce.Shopping.Domain/ECommerce.Shopping.Domain.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

rm Shopping/ECommerce.Shopping.Domain/Class1.cs
```

### 2. Create domain errors

**File: `Shopping/ECommerce.Shopping.Domain/Errors/ShoppingErrors.cs`**

```csharp
using ECommerce.SharedKernel.Results;

namespace ECommerce.Shopping.Domain.Errors;

public static class ShoppingErrors
{
    // Cart
    public static readonly DomainError QuantityInvalid  = new("QUANTITY_INVALID",   "Quantity must be greater than zero.");
    public static readonly DomainError CartFull         = new("CART_FULL",          "Cart cannot hold more than 50 distinct items.");
    public static readonly DomainError CartItemNotFound = new("CART_ITEM_NOT_FOUND","Cart item not found.");

    // Wishlist
    public static readonly DomainError WishlistFull     = new("WISHLIST_FULL",      "Wishlist cannot hold more than 100 products.");

    // NOTE: CartNotFound, WishlistNotFound, ProductNotFound require repo lookups —
    // they live in ShoppingApplicationErrors (step-2), not here.
}
```

### 3. Create domain events

**File: `Shopping/ECommerce.Shopping.Domain/Events/ItemAddedToCartEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Events;

public record ItemAddedToCartEvent(
    Guid CartId,
    Guid ProductId,
    int  Quantity
) : DomainEventBase;
```

**File: `Shopping/ECommerce.Shopping.Domain/Events/CartItemQuantityUpdatedEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Events;

public record CartItemQuantityUpdatedEvent(
    Guid CartId,
    Guid ProductId,
    int  NewQuantity
) : DomainEventBase;
```

**File: `Shopping/ECommerce.Shopping.Domain/Events/CartClearedEvent.cs`**
```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Events;

public record CartClearedEvent(
    Guid CartId,
    Guid UserId
) : DomainEventBase;
```

### 4. Create AssemblyInfo

**File: `Shopping/ECommerce.Shopping.Domain/Properties/AssemblyInfo.cs`**

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ECommerce.Shopping.Infrastructure")]
```

### 5. Create CartItem child entity

**File: `Shopping/ECommerce.Shopping.Domain/Aggregates/Cart/CartItem.cs`**

```csharp
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Aggregates.Cart;

// public sealed with internal constructor — type visible to Application (it reads CartItem properties),
// but only Cart.AddItem() can create instances.
public sealed class CartItem : Entity
{
    public Guid    CartId    { get; private set; }
    public Guid    ProductId { get; private set; }  // ID only — no navigation to Product
    public int     Quantity  { get; private set; }
    public decimal UnitPrice { get; private set; }  // Snapshot: price at add time, not updated
    public string  Currency  { get; private set; } = null!;

    private CartItem() { } // EF Core

    internal static CartItem Create(
        Guid id, Guid cartId, Guid productId,
        int quantity, decimal unitPrice, string currency)
        => new()
        {
            Id        = id,
            CartId    = cartId,
            ProductId = productId,
            Quantity  = quantity,
            UnitPrice = unitPrice,
            Currency  = currency,
        };

    internal void IncreaseQuantity(int amount) => Quantity += amount;
    internal void SetQuantity(int quantity)    => Quantity = quantity;
}
```

### 6. Create Cart aggregate

**File: `Shopping/ECommerce.Shopping.Domain/Aggregates/Cart/Cart.cs`**

```csharp
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Domain.Errors;
using ECommerce.Shopping.Domain.Events;

namespace ECommerce.Shopping.Domain.Aggregates.Cart;

public sealed class Cart : AggregateRoot
{
    public Guid   UserId     { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>(); // concurrency token

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public int  ItemCount => _items.Sum(i => i.Quantity);
    public bool IsEmpty   => _items.Count == 0;

    public decimal Subtotal => _items.Sum(i => i.UnitPrice * i.Quantity);

    private Cart() { }

    public static Cart Create(Guid userId)
        => new()
        {
            Id     = Guid.NewGuid(),
            UserId = userId,
        };

    /// <summary>
    /// Idempotent: adding a product already in the cart increases its quantity.
    /// Only adds a new CartItem if the product is not already present.
    /// </summary>
    public Result AddItem(Guid productId, int quantity, decimal unitPrice, string currency)
    {
        if (quantity <= 0)
            return Result.Fail(ShoppingErrors.QuantityInvalid);

        CartItem? existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            AddDomainEvent(new CartItemQuantityUpdatedEvent(Id, productId, existing.Quantity));
            return Result.Ok();
        }

        if (_items.Count >= 50)
            return Result.Fail(ShoppingErrors.CartFull);

        _items.Add(CartItem.Create(Guid.NewGuid(), Id, productId, quantity, unitPrice, currency));
        AddDomainEvent(new ItemAddedToCartEvent(Id, productId, quantity));
        return Result.Ok();
    }

    public Result UpdateItemQuantity(Guid cartItemId, int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Fail(ShoppingErrors.QuantityInvalid);

        CartItem? item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null) return Result.Fail(ShoppingErrors.CartItemNotFound);

        item.SetQuantity(newQuantity);
        AddDomainEvent(new CartItemQuantityUpdatedEvent(Id, item.ProductId, newQuantity));
        return Result.Ok();
    }

    public Result RemoveItem(Guid cartItemId)
    {
        CartItem? item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null) return Result.Fail(ShoppingErrors.CartItemNotFound);

        _items.Remove(item);
        return Result.Ok();
    }

    public void Clear()
    {
        _items.Clear();
        AddDomainEvent(new CartClearedEvent(Id, UserId));
    }
}
```

**Design notes:**
- `RowVersion` is `byte[]` — EF configures it as a concurrency token (`IsRowVersion()`). The aggregate exposes it as a property so Infrastructure can map it.
- `Subtotal` is a computed property from snaphotted prices. No separate domain event for price changes — the snapshot is intentional.
- `AddItem` idempotency: the check is `_items.FirstOrDefault(i => i.ProductId == productId)`. The 50-item limit only applies when adding a NEW distinct product, not when increasing quantity of an existing one.

### 7. Create Wishlist aggregate

**File: `Shopping/ECommerce.Shopping.Domain/Aggregates/Wishlist/Wishlist.cs`**

```csharp
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Domain.Errors;

namespace ECommerce.Shopping.Domain.Aggregates.Wishlist;

// Wishlist stores a list of product IDs — no child entity because product IDs have no properties.
// EF Core maps the List<Guid> via WishlistItems join table (see Infrastructure).
public sealed class Wishlist : AggregateRoot
{
    public Guid UserId { get; private set; }

    private readonly List<Guid> _productIds = new();
    public IReadOnlyCollection<Guid> ProductIds => _productIds.AsReadOnly();

    private Wishlist() { }

    public static Wishlist Create(Guid userId)
        => new()
        {
            Id     = Guid.NewGuid(),
            UserId = userId,
        };

    /// <summary>Idempotent: adding an already-present product is a no-op (returns Ok).</summary>
    public Result AddProduct(Guid productId)
    {
        if (_productIds.Contains(productId)) return Result.Ok();
        if (_productIds.Count >= 100)
            return Result.Fail(ShoppingErrors.WishlistFull);
        _productIds.Add(productId);
        return Result.Ok();
    }

    /// <summary>No-op if product is not present — never fails.</summary>
    public void RemoveProduct(Guid productId) => _productIds.Remove(productId);

    public bool Contains(Guid productId) => _productIds.Contains(productId);

    public void Clear() => _productIds.Clear();
}
```

**Why `List<Guid>` not a child entity?** Product IDs in a wishlist have no properties of their own. There is no `WishlistItem.Quantity`, no `WishlistItem.AddedAt` tracked by the domain. The EF persistence layer uses a join table for storage, but that's an infrastructure detail.

### 8. Create repository interfaces

**File: `Shopping/ECommerce.Shopping.Domain/Interfaces/ICartRepository.cs`**

```csharp
using ECommerce.Shopping.Domain.Aggregates.Cart;

namespace ECommerce.Shopping.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?>  GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Cart?>  GetByIdAsync(Guid cartId, CancellationToken ct = default);
    Task UpsertAsync(Cart cart, CancellationToken ct = default);
    Task DeleteAsync(Cart cart, CancellationToken ct = default);
}
```

**File: `Shopping/ECommerce.Shopping.Domain/Interfaces/IWishlistRepository.cs`**

```csharp
using ECommerce.Shopping.Domain.Aggregates.Wishlist;

namespace ECommerce.Shopping.Domain.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default);
}
```

### 9. Verify

```bash
cd src/backend
dotnet build Shopping/ECommerce.Shopping.Domain/ECommerce.Shopping.Domain.csproj
dotnet build  # Entire solution still builds
```

---

## Tester Handoff

Once this step is delivered, the tester writes domain unit tests in `ECommerce.Shopping.Tests/Domain/`. See `step-5-domain-tests.md`.

---

## Acceptance Criteria

- [ ] `ECommerce.Shopping.Domain` project created and added to solution
- [ ] Only dependency: `ECommerce.SharedKernel`
- [ ] `Cart` aggregate: `Create`, `AddItem` (idempotent), `UpdateItemQuantity`, `RemoveItem`, `Clear`
- [ ] `CartItem`: `public sealed class` with `internal` constructor and factory, `internal` mutation methods
- [ ] `Cart.RowVersion` property (`byte[]`) — concurrency token for EF
- [ ] `Cart.Subtotal` computed from snapshot prices
- [ ] `Wishlist` aggregate: `Create`, `AddProduct` (idempotent no-op), `RemoveProduct` (no-op if absent), `Contains`, `Clear`
- [ ] `Wishlist._productIds` is `List<Guid>` — NOT a list of child entities
- [ ] 3 domain events: `ItemAddedToCartEvent`, `CartItemQuantityUpdatedEvent`, `CartClearedEvent`
- [ ] `ShoppingErrors` has NO `CartNotFound`/`WishlistNotFound`/`ProductNotFound` — those live in Application
- [ ] `ICartRepository` and `IWishlistRepository` interfaces defined
- [ ] `AssemblyInfo.cs` with `InternalsVisibleTo("ECommerce.Shopping.Infrastructure")`
- [ ] `dotnet build` passes
